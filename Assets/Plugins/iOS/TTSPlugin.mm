#import <AVFoundation/AVFoundation.h>

static AVSpeechSynthesizer *synth = nil;
static BOOL isTTSReady = NO;

extern "C" {
    void _InitTTS() {
        if (!synth) synth = [[AVSpeechSynthesizer alloc] init];
        isTTSReady = YES;
    }

    bool _IsTTSReady() {
        return isTTSReady;
    }

    void _SpeakWithLanguage(const char* text, const char* langCode) {
        if (!synth) {
            NSLog(@"[TTSPlugin] TTS not initialized");
            return;
        }

        NSString *nsText = [NSString stringWithUTF8String:text];
        NSString *nsLang = [NSString stringWithUTF8String:langCode];

        AVSpeechUtterance *utterance = [AVSpeechUtterance speechUtteranceWithString:nsText];
        AVSpeechSynthesisVoice *voice = [AVSpeechSynthesisVoice voiceWithLanguage:nsLang];
        if (voice != nil) {
            utterance.voice = voice;
        } else {
            NSLog(@"[TTSPlugin] Language %@ not supported. Using default voice.", nsLang);
        }

        [synth speakUtterance:utterance];
    }

    void _StopTTS() {
        if (synth) [synth stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
    }
}
