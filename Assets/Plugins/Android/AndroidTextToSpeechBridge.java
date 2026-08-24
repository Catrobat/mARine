// package com.demo.marine.speech;

import android.app.Activity;
import android.os.Bundle;
import android.os.Looper;
import android.os.Handler;
import android.speech.tts.TextToSpeech;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import java.util.Locale;
import java.util.UUID;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;

public class AndroidTextToSpeechBridge
{
    private static final String TAG = "MarineTTS";
    private static final long SPEAK_RETRY_DELAY_MS = 180L;

    private TextToSpeech _tts;
    private boolean _ready;
    private String _languageTag = "en-US";
    private String _pendingText;
    private String _pendingLanguageTag;
    private int _speakRetryCount;
    private final Handler _mainHandler = new Handler(Looper.getMainLooper());

    public boolean initialize(String languageTag)
    {
        if (languageTag != null && !languageTag.trim().isEmpty())
            _languageTag = normalizeLanguageTag(languageTag);

        if (_tts != null && _ready)
        {
            setLanguage(_languageTag);
            return true;
        }

        Activity activity = UnityPlayer.currentActivity;
        if (activity == null)
        {
            Log.e(TAG, "Cannot initialize TextToSpeech: Unity currentActivity is null.");
            return false;
        }

        AtomicBoolean initialized = new AtomicBoolean(false);
        CountDownLatch latch = new CountDownLatch(1);

        Runnable initAction = new Runnable()
        {
            @Override
            public void run()
            {
                try
                {
                    shutdownOnUiThread();
                    _tts = new TextToSpeech(activity.getApplicationContext(), new TextToSpeech.OnInitListener()
                    {
                        @Override
                        public void onInit(int status)
                        {
                            _ready = status == TextToSpeech.SUCCESS;
                            if (_ready)
                            {
                                setLanguage(_languageTag);
                                _mainHandler.postDelayed(new Runnable()
                                {
                                    @Override
                                    public void run()
                                    {
                                        AndroidTextToSpeechBridge.this.speakPendingOnUiThread();
                                    }
                                }, SPEAK_RETRY_DELAY_MS);
                            }
                            else
                            {
                                Log.e(TAG, "TextToSpeech init failed with status " + status);
                            }

                            initialized.set(_ready);
                            latch.countDown();
                        }
                    });
                }
                catch (Exception ex)
                {
                    Log.e(TAG, "Failed to create TextToSpeech.", ex);
                    latch.countDown();
                }
            }
        };

        boolean onMainThread = Looper.myLooper() == Looper.getMainLooper();
        if (onMainThread)
            initAction.run();
        else
            activity.runOnUiThread(initAction);

        if (onMainThread)
            return _tts != null;

        try
        {
            if (!latch.await(5, TimeUnit.SECONDS))
            {
                Log.e(TAG, "TextToSpeech init timed out.");
                return false;
            }
        }
        catch (InterruptedException ex)
        {
            Thread.currentThread().interrupt();
            Log.e(TAG, "TextToSpeech init interrupted.", ex);
            return false;
        }

        return initialized.get();
    }

    public boolean speak(String text, String languageTag)
    {
        if (text == null || text.trim().isEmpty())
            return true;

        if (!_ready || _tts == null)
        {
            _pendingText = text;
            _pendingLanguageTag = languageTag;
            _speakRetryCount = 0;

            if (_tts == null && !initialize(languageTag))
                return false;

            return true;
        }

        if (languageTag != null && !languageTag.trim().isEmpty())
        {
            String normalized = normalizeLanguageTag(languageTag);
            if (!_languageTag.equals(normalized))
            {
                _languageTag = normalized;
                setLanguage(_languageTag);
            }
        }

        Activity activity = UnityPlayer.currentActivity;
        if (activity == null)
        {
            Log.e(TAG, "Cannot speak: Unity currentActivity is null.");
            return false;
        }

        AtomicBoolean result = new AtomicBoolean(false);
        CountDownLatch latch = new CountDownLatch(1);

        Runnable speakAction = new Runnable()
        {
            @Override
            public void run()
            {
                try
                {
                    Bundle params = new Bundle();
                    String utteranceId = "marine-tts-" + UUID.randomUUID();
                    int speakResult = _tts.speak(
                        text,
                        TextToSpeech.QUEUE_FLUSH,
                        params,
                        utteranceId);

                    if (speakResult != TextToSpeech.SUCCESS)
                    {
                        Log.e(TAG, "TextToSpeech.speak failed with result " + speakResult);
                        result.set(scheduleRetry(text, languageTag));
                        return;
                    }

                    result.set(speakResult == TextToSpeech.SUCCESS);
                }
                catch (Exception ex)
                {
                    Log.e(TAG, "TextToSpeech.speak threw.", ex);
                    result.set(scheduleRetry(text, languageTag));
                }
                finally
                {
                    latch.countDown();
                }
            }
        };

        if (Looper.myLooper() == Looper.getMainLooper())
            speakAction.run();
        else
            activity.runOnUiThread(speakAction);

        try
        {
            if (!latch.await(3, TimeUnit.SECONDS))
            {
                Log.e(TAG, "TextToSpeech speak timed out on UI thread.");
                return false;
            }
        }
        catch (InterruptedException ex)
        {
            Thread.currentThread().interrupt();
            Log.e(TAG, "TextToSpeech speak interrupted.", ex);
            return false;
        }

        return result.get();
    }

    public boolean isReady()
    {
        return _ready && _tts != null;
    }

    public void stop()
    {
        Activity activity = UnityPlayer.currentActivity;
        if (activity == null)
        {
            stopOnUiThread();
            return;
        }

        Runnable stopAction = new Runnable()
        {
            @Override
            public void run()
            {
                stopOnUiThread();
            }
        };

        if (Looper.myLooper() == Looper.getMainLooper())
            stopAction.run();
        else
            activity.runOnUiThread(stopAction);
    }

    public void shutdown()
    {
        Activity activity = UnityPlayer.currentActivity;
        if (activity == null)
        {
            shutdownOnUiThread();
            return;
        }

        Runnable shutdownAction = new Runnable()
        {
            @Override
            public void run()
            {
                shutdownOnUiThread();
            }
        };

        if (Looper.myLooper() == Looper.getMainLooper())
            shutdownAction.run();
        else
            activity.runOnUiThread(shutdownAction);
    }

    private void setLanguage(String languageTag)
    {
        if (_tts == null)
            return;

        Locale locale = Locale.forLanguageTag(normalizeLanguageTag(languageTag));
        int result = _tts.setLanguage(locale);
        if (result == TextToSpeech.LANG_MISSING_DATA || result == TextToSpeech.LANG_NOT_SUPPORTED)
        {
            Log.w(TAG, "Language " + languageTag + " unavailable; falling back to default engine language.");
        }
    }

    private void speakPendingOnUiThread()
    {
        if (_pendingText == null || _pendingText.trim().isEmpty())
            return;

        String text = _pendingText;
        String languageTag = _pendingLanguageTag;
        _pendingText = null;
        _pendingLanguageTag = null;

        if (languageTag != null && !languageTag.trim().isEmpty())
        {
            _languageTag = normalizeLanguageTag(languageTag);
            setLanguage(_languageTag);
        }

        try
        {
            Bundle params = new Bundle();
            int result = _tts.speak(
                text,
                TextToSpeech.QUEUE_FLUSH,
                params,
                "marine-tts-" + UUID.randomUUID());

            if (result != TextToSpeech.SUCCESS)
            {
                Log.e(TAG, "Pending TextToSpeech.speak failed with result " + result);
                scheduleRetry(text, languageTag);
            }
        }
        catch (Exception ex)
        {
            Log.e(TAG, "Pending TextToSpeech.speak threw.", ex);
            scheduleRetry(text, languageTag);
        }
    }

    private boolean scheduleRetry(final String text, final String languageTag)
    {
        if (_speakRetryCount >= 1)
        {
            Log.e(TAG, "TextToSpeech speak failed after retry.");
            return false;
        }

        _speakRetryCount++;
        _pendingText = text;
        _pendingLanguageTag = languageTag;
        _mainHandler.postDelayed(new Runnable()
        {
            @Override
            public void run()
            {
                speakPendingOnUiThread();
            }
        }, SPEAK_RETRY_DELAY_MS);
        return true;
    }

    private void stopOnUiThread()
    {
        if (_tts == null)
            return;

        try
        {
            _tts.stop();
        }
        catch (Exception ex)
        {
            Log.w(TAG, "TextToSpeech stop failed.", ex);
        }
    }

    private void shutdownOnUiThread()
    {
        if (_tts == null)
            return;

        try
        {
            _tts.stop();
            _tts.shutdown();
        }
        catch (Exception ex)
        {
            Log.w(TAG, "TextToSpeech shutdown failed.", ex);
        }

        _tts = null;
        _ready = false;
    }

    private static String normalizeLanguageTag(String languageTag)
    {
        if (languageTag == null || languageTag.trim().isEmpty())
            return "en-US";

        return languageTag.trim().replace('_', '-');
    }
}
