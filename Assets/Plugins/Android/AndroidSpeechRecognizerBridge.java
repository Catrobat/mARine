// package com.demo.marine.speech;

import android.app.Activity;
import android.content.ComponentName;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.content.pm.ResolveInfo;
import android.os.Build;
import android.os.Bundle;
import android.speech.RecognitionListener;
import android.speech.RecognitionService;
import android.speech.RecognizerIntent;
import android.speech.SpeechRecognizer;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;

public class AndroidSpeechRecognizerBridge
{
    private static final String TAG = "MarineSpeech";
    private SpeechRecognizer _recognizer;
    private boolean _listening;
    private boolean _retriedWithDefaultSettings;
    private String _gameObjectName;
    private String _onResultMethod;
    private String _onErrorMethod;
    private String _languageTag;
    private boolean _preferOffline;
    private String[] _biasingPhrases;

    public boolean startListening(
        String gameObjectName,
        String onResultMethod,
        String onErrorMethod,
        String languageTag,
        boolean preferOffline,
        String[] biasingPhrases)
    {
        _gameObjectName = gameObjectName;
        _onResultMethod = onResultMethod;
        _onErrorMethod = onErrorMethod;
        _languageTag = languageTag;
        _preferOffline = preferOffline;
        _biasingPhrases = biasingPhrases;
        _retriedWithDefaultSettings = false;

        if (_listening)
            return true;

        Activity activity = UnityPlayer.currentActivity;
        if (activity == null)
        {
            sendError("Current activity is null.");
            return false;
        }

        AtomicBoolean started = new AtomicBoolean(false);
        CountDownLatch latch = new CountDownLatch(1);

        activity.runOnUiThread(new Runnable()
        {
            @Override
            public void run()
            {
                try
                {
                    started.set(startListeningOnUiThread(activity));
                }
                finally
                {
                    latch.countDown();
                }
            }
        });

        try
        {
            if (!latch.await(3, TimeUnit.SECONDS))
            {
                sendError("Speech recognizer start timed out on Android UI thread.");
                return false;
            }
        }
        catch (InterruptedException ex)
        {
            Thread.currentThread().interrupt();
            sendError("Speech recognizer start was interrupted.");
            return false;
        }

        return started.get();
    }

    private boolean startListeningOnUiThread(Activity activity)
    {
        if (!SpeechRecognizer.isRecognitionAvailable(activity))
        {
            sendError("Speech recognition is not available on this device.");
            return false;
        }

        try
        {
            _recognizer = createRecognizer(activity);
        }
        catch (Exception ex)
        {
            sendError(
                "No usable Android speech recognizer could be created. recognitionAvailable=" +
                SpeechRecognizer.isRecognitionAvailable(activity) +
                ", onDeviceAvailable=" +
                (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S
                    && SpeechRecognizer.isOnDeviceRecognitionAvailable(activity)) +
                ", services=" + countRecognitionServices(activity) +
                ", error=" + ex.getClass().getSimpleName() +
                ": " + ex.getMessage());
            return false;
        }

        _recognizer.setRecognitionListener(new RecognitionListener()
        {
            @Override
            public void onReadyForSpeech(Bundle params)
            {
            }

            @Override
            public void onBeginningOfSpeech()
            {
            }

            @Override
            public void onRmsChanged(float rmsdB)
            {
            }

            @Override
            public void onBufferReceived(byte[] buffer)
            {
            }

            @Override
            public void onEndOfSpeech()
            {
            }

            @Override
            public void onError(int error)
            {
                _listening = false;
                if (shouldRetryWithDefaultSettings(error)
                    && retryWithDefaultSettings())
                {
                    return;
                }

                sendError("ERROR_" + errorToString(error));
                destroyRecognizer();
            }

            @Override
            public void onResults(Bundle results)
            {
                _listening = false;
                sendResult(extractBestResult(results));
                destroyRecognizer();
            }

            @Override
            public void onPartialResults(Bundle partialResults)
            {
            }

            @Override
            public void onEvent(int eventType, Bundle params)
            {
            }
        });

        Intent intent = createRecognizerIntent(
            _languageTag,
            _preferOffline,
            _biasingPhrases);

        try
        {
            _recognizer.startListening(intent);
            _listening = true;
            return true;
        }
        catch (Exception ex)
        {
            sendError("Failed to start listening: " + ex.getMessage());
            destroyRecognizer();
            return false;
        }
    }

    private boolean shouldRetryWithDefaultSettings(int error)
    {
        return !_retriedWithDefaultSettings
            && (error == 11 || error == 12 || error == 13);
    }

    private boolean retryWithDefaultSettings()
    {
        Activity activity = UnityPlayer.currentActivity;
        if (activity == null)
            return false;

        _retriedWithDefaultSettings = true;
        destroyRecognizer();

        try
        {
            _recognizer = createStandardRecognizer(activity);
            _recognizer.setRecognitionListener(new RecognitionListener()
            {
                @Override
                public void onReadyForSpeech(Bundle params)
                {
                }

                @Override
                public void onBeginningOfSpeech()
                {
                }

                @Override
                public void onRmsChanged(float rmsdB)
                {
                }

                @Override
                public void onBufferReceived(byte[] buffer)
                {
                }

                @Override
                public void onEndOfSpeech()
                {
                }

                @Override
                public void onError(int error)
                {
                    _listening = false;
                    sendError("ERROR_" + errorToString(error));
                    destroyRecognizer();
                }

                @Override
                public void onResults(Bundle results)
                {
                    _listening = false;
                    sendResult(extractBestResult(results));
                    destroyRecognizer();
                }

                @Override
                public void onPartialResults(Bundle partialResults)
                {
                }

                @Override
                public void onEvent(int eventType, Bundle params)
                {
                }
            });

            _recognizer.startListening(createRecognizerIntent(null, false, null));
            _listening = true;
            Log.i(TAG, "Retried speech recognition with default language and online fallback.");
            return true;
        }
        catch (Exception ex)
        {
            sendError("Retry with default Android speech settings failed: " + ex.getMessage());
            destroyRecognizer();
            return false;
        }
    }

    private static Intent createRecognizerIntent(
        String languageTag,
        boolean preferOffline,
        String[] biasingPhrases)
    {
        Intent intent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
        intent.putExtra(
            RecognizerIntent.EXTRA_LANGUAGE_MODEL,
            RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);
        intent.putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, 1);
        intent.putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, false);

        if (preferOffline)
            intent.putExtra(RecognizerIntent.EXTRA_PREFER_OFFLINE, true);

        if (languageTag != null && !languageTag.trim().isEmpty())
            intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE, languageTag);

        if (biasingPhrases != null && biasingPhrases.length > 0)
        {
            ArrayList<String> phrases = new ArrayList<String>();
            for (String phrase : biasingPhrases)
            {
                if (phrase != null && !phrase.trim().isEmpty())
                    phrases.add(phrase.trim());
            }

            if (!phrases.isEmpty())
            {
                intent.putStringArrayListExtra(
                    "android.speech.extra.BIASING_STRINGS",
                    phrases);
            }
        }

        return intent;
    }

    private static SpeechRecognizer createRecognizer(Activity activity)
    {
        // The explicit on-device recognizer is fragile across vendor builds.
        // Use the standard recognizer and request offline mode through the intent.
        return createStandardRecognizer(activity);
    }

    private static SpeechRecognizer createStandardRecognizer(Activity activity)
    {
        SpeechRecognizer recognizer = SpeechRecognizer.createSpeechRecognizer(activity);
        Log.i(TAG, "Created default speech recognizer.");
        return recognizer;
    }

    private static ComponentName findRecognitionService(Activity activity)
    {
        try
        {
            PackageManager pm = activity.getPackageManager();
            Intent intent = new Intent(RecognitionService.SERVICE_INTERFACE);
            List<ResolveInfo> services = pm.queryIntentServices(intent, 0);

            if (services == null || services.isEmpty())
            {
                Log.w(TAG, "No recognition services found via PackageManager.");
                return null;
            }

            ResolveInfo info = services.get(0);
            if (info.serviceInfo == null)
                return null;

            ComponentName service = new ComponentName(
                info.serviceInfo.packageName,
                info.serviceInfo.name);

            Log.i(TAG, "Using recognition service: " + service);
            return service;
        }
        catch (Exception ex)
        {
            Log.w(TAG, "Failed to query recognition services.", ex);
            return null;
        }
    }

    private static int countRecognitionServices(Activity activity)
    {
        try
        {
            PackageManager pm = activity.getPackageManager();
            Intent intent = new Intent(RecognitionService.SERVICE_INTERFACE);
            List<ResolveInfo> services = pm.queryIntentServices(intent, 0);
            return services == null ? 0 : services.size();
        }
        catch (Exception ex)
        {
            return -1;
        }
    }

    public void stopListening()
    {
        Activity activity = UnityPlayer.currentActivity;
        if (activity == null)
        {
            stopListeningOnUiThread();
            return;
        }

        activity.runOnUiThread(new Runnable()
        {
            @Override
            public void run()
            {
                stopListeningOnUiThread();
            }
        });
    }

    public void cancel()
    {
        Activity activity = UnityPlayer.currentActivity;
        if (activity == null)
        {
            cancelOnUiThread();
            return;
        }

        activity.runOnUiThread(new Runnable()
        {
            @Override
            public void run()
            {
                cancelOnUiThread();
            }
        });
    }

    private void stopListeningOnUiThread()
    {
        if (_recognizer == null || !_listening)
            return;

        try
        {
            _recognizer.stopListening();
        }
        catch (Exception ex)
        {
            sendError("Failed to stop listening: " + ex.getMessage());
            destroyRecognizer();
        }
    }

    private void cancelOnUiThread()
    {
        if (_recognizer == null)
            return;

        try
        {
            _recognizer.cancel();
        }
        catch (Exception ignored)
        {
        }

        destroyRecognizer();
    }

    private void destroyRecognizer()
    {
        if (_recognizer == null)
            return;

        try
        {
            _recognizer.destroy();
        }
        catch (Exception ignored)
        {
        }
        _recognizer = null;
        _listening = false;
    }

    private void sendResult(String text)
    {
        if (_gameObjectName == null || _onResultMethod == null)
            return;

        UnityPlayer.UnitySendMessage(
            _gameObjectName,
            _onResultMethod,
            text == null ? "" : text);
    }

    private void sendError(String message)
    {
        if (_gameObjectName == null || _onErrorMethod == null)
            return;

        UnityPlayer.UnitySendMessage(
            _gameObjectName,
            _onErrorMethod,
            message == null ? "" : message);
    }

    private static String extractBestResult(Bundle results)
    {
        if (results == null)
            return "";

        ArrayList<String> texts = results.getStringArrayList(
            SpeechRecognizer.RESULTS_RECOGNITION);
        if (texts == null || texts.isEmpty())
            return "";

        String text = texts.get(0);
        return text == null ? "" : text.trim();
    }

    private static String errorToString(int error)
    {
        switch (error)
        {
            case SpeechRecognizer.ERROR_AUDIO:
                return "AUDIO";
            case SpeechRecognizer.ERROR_CLIENT:
                return "CLIENT";
            case SpeechRecognizer.ERROR_INSUFFICIENT_PERMISSIONS:
                return "INSUFFICIENT_PERMISSIONS";
            case SpeechRecognizer.ERROR_NETWORK:
                return "NETWORK";
            case SpeechRecognizer.ERROR_NETWORK_TIMEOUT:
                return "NETWORK_TIMEOUT";
            case SpeechRecognizer.ERROR_NO_MATCH:
                return "NO_MATCH";
            case SpeechRecognizer.ERROR_RECOGNIZER_BUSY:
                return "RECOGNIZER_BUSY";
            case SpeechRecognizer.ERROR_SERVER:
                return "SERVER";
            case SpeechRecognizer.ERROR_SPEECH_TIMEOUT:
                return "SPEECH_TIMEOUT";
            case 10:
                return "TOO_MANY_REQUESTS";
            case 11:
                return "SERVER_DISCONNECTED";
            case 12:
                return "LANGUAGE_NOT_SUPPORTED";
            case 13:
                return "LANGUAGE_UNAVAILABLE";
            case 14:
                return "CANNOT_CHECK_SUPPORT";
            case 15:
                return "CANNOT_LISTEN_TO_DOWNLOAD_EVENTS";
            default:
                return "CODE_" + error;
        }
    }
}
