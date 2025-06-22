#import <AVFoundation/AVFoundation.h>

static AVSpeechSynthesizer *synth = nil;

extern "C" {
    void _Speak(const char* text) {
        NSString *nsText = [NSString stringWithUTF8String:text];
        if (!synth) synth = [[AVSpeechSynthesizer alloc] init];
        AVSpeechUtterance *utterance = [AVSpeechUtterance speechUtteranceWithString:nsText];
        [synth speakUtterance:utterance];
    }
    void _StopTTS() {
        if (synth) [synth stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
    }
}
