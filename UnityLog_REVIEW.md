# Unity log review

Paste your Unity Console / Editor log below. Use this file when debugging a play session or reproducing an issue.

---

## Session info (optional)

| Field | Value |
|-------|-------|
| Date | |
| Build / branch | |
| Scene / flow tested | |
| What you were testing | |

---

## Raw log

Mono path[0] = 'C:/Users/robin/AppData/Local/Programs/SoundSelf/resources/appBuilds/win32/SoundSelf Live Sequence_Data/Managed'
Mono config path = 'C:/Users/robin/AppData/Local/Programs/SoundSelf/resources/appBuilds/win32/MonoBleedingEdge/etc'
[Physics::Module] Initialized MultithreadedJobDispatcher with 19 workers.
Initialize engine version: 2022.3.12f1 (4fe6e059c7ef)
[Subsystems] Discovering subsystems at path C:/Users/robin/AppData/Local/Programs/SoundSelf/resources/appBuilds/win32/SoundSelf Live Sequence_Data/UnitySubsystems
GfxDevice: creating device client; threaded=1; jobified=1
Direct3D:
    Version:  Direct3D 11.0 [level 11.1]
    Renderer: NVIDIA GeForce RTX 3070 Ti Laptop GPU (ID=0x24a0)
    Vendor:   NVIDIA
    VRAM:     8018 MB
    Driver:   32.0.15.5597
Begin MonoManager ReloadAssembly
- Loaded All Assemblies, in  3.893 seconds
- Finished resetting the current domain, in  0.001 seconds
<RI> Initializing input.
New input system (experimental) initialized
Using Windows.Gaming.Input
<RI> Input initialized.
<RI> Initialized touch support.
UnloadTime: 0.987200 ms
WwiseUnity: Wwise(R) SDK Version 2023.1.15 Build 8789.
WwiseUnity: Setting Plugin DLL path to: C:/Users/robin/AppData/Local/Programs/SoundSelf/resources/appBuilds/win32/SoundSelf Live Sequence_Data\Plugins\x86_64
WwiseUnity: Sound engine initialized successfully.
Recording: Initialized recording folders at C:/Users/robin/AppData/LocalLow/Entheogen Limited/SoundSelf Live Sequence\RecordedClips\20260520_140041
Base path: C:\Users\robin\AppData\Roaming\Hummingbird\StreamingAssets\Resources
CSV file found at: C:\Users\robin\AppData\Roaming\Hummingbird\StreamingAssets\Resources\session_27497882\session_params.csv
Encrypted Game Mode: 015eccafdb39c67fc1d99bcb696ca747:3ff9dc4b34930cfc40af41539104a922
Encrypted Content Pack: d898231a7a4c7d9de1279c93492af2a7:90e5ab1f08d1ca1f3158e7960883abc7b7b393a570dd3523c6e9e54deaceeabb
Encrypted First Time User: dcde21c39ebece6bd8d962ba4db84535:698f34ed8c6210a4fc1fd7df5af36055
Encrypted Laying Down: dcc818f15be8e4729bfae5e7b59f829d:61a701107fa355de5cd581aaae4bd4a8
Encrypted Vibroacoustic: 0576932ba177bcff635353e27bfb63e2:4f092ac009033f822d57f0625418871a
CSVLoader: modes set to: Game Mode(Sonoflore) Content Pack(Mindfulness and Joy)
WWise_VO: Set to Peace
CSVLoader: TimeLeftInitializations() - tracker inputs set. totalTimeOfPostUnguidedVocalizationContent=840 s. Session countdown is unchanged until StartCountdown → BeginCountdownPair (current [CountdownThisSection]=1000000 [CountdownFull]=1000000). SessionTimingInitializedFromCsv=True.
AWAKE IN PRODUCTION MODE
MUSIC: AkGameObj component already exists
Binaural Beats: Initializing Binaural Beats Manager
AVSSequence: Multiple AVSSequence instances; destroying duplicate.
Imitone: Chose microphone: Headset Microphone (CORSAIR HS80 RGB Wireless Gaming Receiver)
Atonal interval: 360 ... transients interval 1080
SAHIR Rack deployment 1 initiated
Initial query to /tracker/1/tones: 200
Initial query to /tracker/1/metrics: 200
Initial query to /transcriber/1/notes: 200
DirectVoiceMonitoring: Monitoring started.
DirectVoiceMonitoring: Monitoring initialized from ImitoneVoiceIntepreter (Normalized stream).
TimeTrackerScript: Start called. Time.timeScale = 1
RTPC Wave1 Frequency after initialization: 1
Devices not found
Device found: Speakers (MPL Audio       ) With ID: 554469992
OutputSettings2 ID Device 554469992
GameObjectID : 4102
CSVLoader: Setting VO Posture to LieDown
WWise_VO: Stop Opening Sequence
Tutorial: StopTutorial called, but tutorial is not active.
WWise_VO: Stop Music Playlist
SequenceRunner.StartSequence: "Sonoflore Sequence" (9 stages) — first stage index 0: SetMenu / Menu_Welcome_PreCalibration.
Preferred color set to: Dark
Transition to Dark over 5 s with new currentColorType of Dark
Going Dark - 5
WWise_VO: Stop Opening Sequence
SetMenuStageHandler: Enter Menu_Welcome_PreCalibration.
MUSIC_LINEAR: Play — State=Environment, PostEvent Play_AMBIENT_ENVIRONMENT_LOOP
Sequence: Entered stage 0 (SetMenu)
Current session number: 27497882
DirectVoiceMonitoring: Auto-reset reliability telemetry after startup delay.
[Step3a-pivot] cursor primed: latencyTarget=64.0ms, readPos=120320, readTotal=120320, writeTotal=123392, gap=3072sa (~64.0ms), ringFill=123392sa
TimeTrackerScript: [tick] 0:05    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
SequenceRunner.TryExecute EndThisSequenceStage: currentIdx=0 currentStage=SetMenu currentHandler=SetMenuStageHandler currentWatches=True; transitioningIdx=-1 transitioningHandler=null transitioningWatches=False.
SetMenuStageHandler: Marking stage complete.
SequenceRunner.TryExecute EndThisSequenceStage: handled=True.
EndThisSequenceStage: Handled by sequence stages (current and/or transitioning-out).
SequenceRunner.TryAdvanceIfCurrentStageComplete: advancing from stage index 0 (SetMenu).
MUSIC_LINEAR: Stop — State=Music, scheduling Stop_AMBIENT_ENVIRONMENT_LOOP in 10.0s
CalibrationStageHandler: variant Calibration_Default (default full calibration path).
CalibrationStageHandler: Enter — step count 6, first screen Start
Sequence: Entered stage 1 (Calibration)
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=0, switch=0) hardSteps=0 source=Normalized enabled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Instruction_ON'
CalibrationInstructionVoStarted: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Instruction_ON' → CalibrationInstructionVoStarted; HandleSequenceCommand handled=True
TimeTrackerScript: [tick] 0:10    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationStageHandler: Start Next pressed; loading spinner shown. Auto-advance happens on Cue_Calibration_Intro_End.
TimeTrackerScript: [tick] 0:15    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
MUSIC_LINEAR: delayed — PostEvent Stop_AMBIENT_ENVIRONMENT_LOOP
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=0, switch=0) hardSteps=0 source=Normalized enabled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
TimeTrackerScript: [tick] 0:20    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
TimeTrackerScript: [tick] 0:25    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Instruction_OFF'
CalibrationInstructionVoEnded: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Instruction_OFF' → CalibrationInstructionVoEnded; HandleSequenceCommand handled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Intro_End'
CalibrationStageHandler: Cue_Calibration_Intro_End → auto-advance from Start.
CalibrationStageHandler: Next step → index 1 screen Headphone
CalibrationIntroEnded: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Intro_End' → CalibrationIntroEnded; HandleSequenceCommand handled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Instruction_ON'
CalibrationInstructionVoStarted: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Instruction_ON' → CalibrationInstructionVoStarted; HandleSequenceCommand handled=True
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=0, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 0:30    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
TimeTrackerScript: [tick] 0:35    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=0, switch=0) hardSteps=0 source=Normalized enabled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Instruction_OFF'
CalibrationStageHandler: Pending Next unlocked (Wwise mirror) → index 2 screen Microphone
CalibrationInstructionVoEnded: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Instruction_OFF' → CalibrationInstructionVoEnded; HandleSequenceCommand handled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Instruction_ON'
CalibrationInstructionVoStarted: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Instruction_ON' → CalibrationInstructionVoStarted; HandleSequenceCommand handled=True
TimeTrackerScript: [tick] 0:40    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Microphone_ON'
CalibrationMicrophoneOn fired but it's not being watched for, so nothing is happening.
CalibrationMenu: MusicSyncUserCue 'Cue_Microphone_ON' → CalibrationMicrophoneOn; HandleSequenceCommand handled=False
TimeTrackerScript: [tick] 0:45    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=0, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 0:50    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Instruction_OFF'
CalibrationStageHandler: Pending Next unlocked (Wwise mirror) → index 3 screen VibroAcoustic
CalibrationInstructionVoEnded: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Instruction_OFF' → CalibrationInstructionVoEnded; HandleSequenceCommand handled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Microphone_OFF'
CalibrationMicrophoneOff fired but it's not being watched for, so nothing is happening.
CalibrationMenu: MusicSyncUserCue 'Cue_Microphone_OFF' → CalibrationMicrophoneOff; HandleSequenceCommand handled=False
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Instruction_ON'
CalibrationInstructionVoStarted: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Instruction_ON' → CalibrationInstructionVoStarted; HandleSequenceCommand handled=True
TimeTrackerScript: [tick] 0:55    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Vibration_Start'
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Vibration_Start' — unmapped, not forwarded to Sequencer.
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=0, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 1:00    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Instruction_OFF'
CalibrationInstructionVoEnded: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Instruction_OFF' → CalibrationInstructionVoEnded; HandleSequenceCommand handled=True
TimeTrackerScript: [tick] 1:05    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=0, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 1:10    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationStageHandler: Next step → index 4 screen LightGlasses
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Microphone_OFF'
CalibrationMicrophoneOff fired but it's not being watched for, so nothing is happening.
CalibrationMenu: MusicSyncUserCue 'Cue_Microphone_OFF' → CalibrationMicrophoneOff; HandleSequenceCommand handled=False
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Instruction_ON'
CalibrationInstructionVoStarted: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Instruction_ON' → CalibrationInstructionVoStarted; HandleSequenceCommand handled=True
TimeTrackerScript: [tick] 1:15    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=0, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 1:20    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
TimeTrackerScript: [tick] 1:25    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=0, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 1:30    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
TimeTrackerScript: [tick] 1:35    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_AVS_Calibration_Start'
Preferred color set to: White
Transition to White2 over 5 s with new currentColorType of White
Binaural Beats: Lerping Binaural Beat Rate from 4 to 5
Strobe Rate set to: 10 Hz immediately
CalibrationAvsStart: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_AVS_Calibration_Start' → CalibrationAvsStart; HandleSequenceCommand handled=True
AVS: Starting Reference Signal
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=0, switch=0) hardSteps=0 source=Normalized enabled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
TimeTrackerScript: [tick] 1:40    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
TimeTrackerScript: [tick] 1:45    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_AVS_Calibration_End'
Preferred color set to: Dark
Transition to Dark over 5 s with new currentColorType of Dark
Going Dark - 5
MusicBinauralBeats: Rate is 0 (<= 0), setting to minimum 4Hz for waveType theta
Binaural Beats: Lerping Binaural Beat Rate from 4.998243 to 4
Strobe Rate set to: 0 Hz over 5000 ms
CalibrationAvsEnd: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_AVS_Calibration_End' → CalibrationAvsEnd; HandleSequenceCommand handled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Instruction_ON'
CalibrationStageHandler: Pending Next unlocked (Wwise mirror) → index 5 screen Conclusion
CalibrationInstructionVoStarted: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Instruction_ON' → CalibrationInstructionVoStarted; HandleSequenceCommand handled=True
Binaural Beats: New Rate is 5
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=0, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 1:50    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
AVS: Stopping Reference Signal
Binaural Beats: New Rate is 4
TimeTrackerScript: [tick] 1:55    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=0, switch=0) hardSteps=0 source=Normalized enabled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
TimeTrackerScript: [tick] 2:00    [CountdownThisSection] 16666:40    [CountdownFull] 16666:40
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Next'
CalibrationPoliteNext: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Next' → CalibrationPoliteNext; HandleSequenceCommand handled=True
CalibrationMenu: MusicSyncUserCue received: userCueName='Cue_Calibration_Instruction_OFF'
CalibrationStageHandler: MarkComplete — calibration stage finished (conclusion confirm; Instruction_OFF when VO was active, or immediate when between lines / dev bypass).
CalibrationInstructionVoEnded: Handled by sequence stages (current and/or transitioning-out).
CalibrationMenu: MusicSyncUserCue 'Cue_Calibration_Instruction_OFF' → CalibrationInstructionVoEnded; HandleSequenceCommand handled=True
[StartCountdown] Enter variant=Countdown_40m_WithSavasana.
[StartCountdown] TimeTracker state before this stage: [CountdownThisSection]=16666:40 (1000000s) [CountdownFull]=16666:40 (1000000s) IsCountdownRunning=False ConfiguredFullAtLastConfigure=0s
[StartCountdown] Duration variant path: TotalTimeOfPostUnguidedVocalizationContent (for WithSavasana math) = 840.0 s 14:00 (840s).
[StartCountdown] 'Countdown_40m_WithSavasana' WithSavasana: base=40:00 (2400s) postUnguided=14:00 (840s) → [CountdownThisSection]=26:00 (1560s) [CountdownFull]=40:00 (2400s).
[StartCountdown] Resolved configured pair for 'Countdown_40m_WithSavasana': [CountdownThisSection] target = 26:00 (1560s) | [CountdownFull] target = 40:00 (2400s).
[StartCountdown] After ConfigureCountdownPair + BeginCountdownPair (live values): [CountdownThisSection]=26:00 (1560s) [CountdownFull]=40:00 (2400s) IsCountdownRunning=True ConfiguredFullAtLastConfigure=2400s
[StartCountdown] Stage complete (IsComplete=true).
Sequence: Entered stage 2 (StartCountdown)
Preferred color set to: Dark
Transition to Dark over 5 s with new currentColorType of Dark
Going Dark - 5
MusicBinauralBeats: Rate is 0 (<= 0), setting to minimum 4Hz for waveType theta
Strobe Rate set to: 0 Hz over 5000 ms
MUSIC: Set Silent Volume to 65
MUSIC: Setting Music Mode to Silent...
Binaural Beats: Lerping Volume from 0 to 0over 5 seconds
MUSIC: InteractiveMusic tried to stop but did not stop because it is not started
MUSIC: Music Mode Set to Silent (WWise: SoundWorld)
WorldShuffler: Excluding Color World -Blue- from shuffle.
WorldShuffler: Excluding Soundscape -Shadow- from shuffle.
OpeningStageHandler: Standard mode detected. Initializing Standard.
MUSIC FUNDAMENTAL-CONTENT-LOCK: Tried to unlock fundamental content, but it was already unlocked
MUSIC: Soundscape Set To: SonoFlore (SoundWorld)
MUSIC TEST: currentInteractionType after SetSoundscape: SoundWorld
OpeningStageHandler: Playing Sonoflore opening sequence.
WWise_VO: Play Opening Sequence: Preparation_Short
WWise_VO: Play Preparation Short Opening Sequence
OpeningStageHandler: Playing Preparation Short Opening Sequence.
Sequencer: IsOpeningMusicPlaying false → true (NotifyOpeningMusicStarted).
Sequence: Entered stage 3 (Opening)
Director Queue: Director Disabled
TimeTrackerScript: [tick] 2:05    [CountdownThisSection] 25:56    [CountdownFull] 39:56
Binaural Beats: New Volume is 0
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 2:10    [CountdownThisSection] 25:51    [CountdownFull] 39:51
TimeTrackerScript: [tick] 2:15    [CountdownThisSection] 25:46    [CountdownFull] 39:46
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 2:20    [CountdownThisSection] 25:41    [CountdownFull] 39:41
TimeTrackerScript: [tick] 2:25    [CountdownThisSection] 25:36    [CountdownFull] 39:36
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 2:30    [CountdownThisSection] 25:31    [CountdownFull] 39:31
TimeTrackerScript: [tick] 2:35    [CountdownThisSection] 25:26    [CountdownFull] 39:26
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 2:40    [CountdownThisSection] 25:21    [CountdownFull] 39:21
TimeTrackerScript: [tick] 2:45    [CountdownThisSection] 25:16    [CountdownFull] 39:16
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 2:50    [CountdownThisSection] 25:10    [CountdownFull] 39:10
TimeTrackerScript: [tick] 2:55    [CountdownThisSection] 25:05    [CountdownFull] 39:05
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 3:00    [CountdownThisSection] 25:00    [CountdownFull] 39:00
TimeTrackerScript: [tick] 3:05    [CountdownThisSection] 24:55    [CountdownFull] 38:55
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 3:10    [CountdownThisSection] 24:50    [CountdownFull] 38:50
TimeTrackerScript: [tick] 3:15    [CountdownThisSection] 24:45    [CountdownFull] 38:45
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_Posture_Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 3:20    [CountdownThisSection] 24:40    [CountdownFull] 38:40
TimeTrackerScript: [tick] 3:25    [CountdownThisSection] 24:35    [CountdownFull] 38:35
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 3:30    [CountdownThisSection] 24:30    [CountdownFull] 38:30
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue Orientation Start
TimeTrackerScript: [tick] 3:35    [CountdownThisSection] 24:25    [CountdownFull] 38:25
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 3:40    [CountdownThisSection] 24:20    [CountdownFull] 38:20
TimeTrackerScript: [tick] 3:45    [CountdownThisSection] 24:15    [CountdownFull] 38:15
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 3:50    [CountdownThisSection] 24:10    [CountdownFull] 38:10
TimeTrackerScript: [tick] 3:55    [CountdownThisSection] 24:05    [CountdownFull] 38:05
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 4:00    [CountdownThisSection] 24:00    [CountdownFull] 38:00
[MicVoiceIngest] FAIL_OBSERVATION composite is now TRUE — flags: FAIL_AUDIO_CALLBACK_GAP_HIGH | micExitReason=unread_zero hzRolling=46.7 maxGapMs=42.86 imitone/callback=0.998 rawLockMisses=20 overflowDrops=0 dbMicSnap=-70.22 imitoneStateStallFrames=19384
[MicVoiceIngest] FAIL_OBSERVATION still TRUE after 0.25s — flags: FAIL_AUDIO_CALLBACK_GAP_HIGH | micExitReason=unread_zero hzRolling=46.7 maxGapMs=42.86 imitone/callback=0.998 rawLockMisses=20 overflowDrops=0 dbMicSnap=-71.44 imitoneStateStallFrames=19426
TimeTrackerScript: [tick] 4:05    [CountdownThisSection] 23:55    [CountdownFull] 37:55
[MicVoiceIngest] FAIL_OBSERVATION still TRUE after 0.76s — flags: FAIL_AUDIO_CALLBACK_GAP_HIGH | micExitReason=copied_samples hzRolling=46.7 maxGapMs=42.86 imitone/callback=0.998 rawLockMisses=20 overflowDrops=0 dbMicSnap=-69.23 imitoneStateStallFrames=19509
[MicVoiceIngest] FAIL_OBSERVATION cleared after 1.01s — flags seen during window: FAIL_AUDIO_CALLBACK_GAP_HIGH 
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 4:10    [CountdownThisSection] 23:50    [CountdownFull] 37:50
TimeTrackerScript: [tick] 4:15    [CountdownThisSection] 23:45    [CountdownFull] 37:45
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 4:20    [CountdownThisSection] 23:40    [CountdownFull] 37:40
TimeTrackerScript: [tick] 4:25    [CountdownThisSection] 23:35    [CountdownFull] 37:35
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_ThematicOpening_Start
TimeTrackerScript: [tick] 4:30    [CountdownThisSection] 23:30    [CountdownFull] 37:30
TimeTrackerScript: [tick] 4:35    [CountdownThisSection] 23:25    [CountdownFull] 37:25
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 4:40    [CountdownThisSection] 23:20    [CountdownFull] 37:20
TimeTrackerScript: [tick] 4:45    [CountdownThisSection] 23:15    [CountdownFull] 37:15
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 4:50    [CountdownThisSection] 23:10    [CountdownFull] 37:10
TimeTrackerScript: [tick] 4:55    [CountdownThisSection] 23:05    [CountdownFull] 37:05
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 5:00    [CountdownThisSection] 22:59    [CountdownFull] 36:59
TimeTrackerScript: [tick] 5:05    [CountdownThisSection] 22:54    [CountdownFull] 36:54
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 5:10    [CountdownThisSection] 22:49    [CountdownFull] 36:49
TimeTrackerScript: [tick] 5:15    [CountdownThisSection] 22:44    [CountdownFull] 36:44
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 5:20    [CountdownThisSection] 22:39    [CountdownFull] 36:39
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Unexpected Cue: AK_MusicSyncUserCue | Cue_ThematicOpening_End
TimeTrackerScript: [tick] 5:25    [CountdownThisSection] 22:34    [CountdownFull] 36:34
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Stopping Openign Seq, play sigh Query Seq
TimeTrackerScript: [tick] 5:30    [CountdownThisSection] 22:29    [CountdownFull] 36:29
TimeTrackerScript: [tick] 5:35    [CountdownThisSection] 22:24    [CountdownFull] 36:24
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 5:40    [CountdownThisSection] 22:19    [CountdownFull] 36:19
TimeTrackerScript: [tick] 5:45    [CountdownThisSection] 22:14    [CountdownFull] 36:14
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue Mic On
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
FXWave command ignored because the current color world is Dark
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 5:50    [CountdownThisSection] 22:09    [CountdownFull] 36:09
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue Sigh Start
TimeTrackerScript: [tick] 5:55    [CountdownThisSection] 22:04    [CountdownFull] 36:04
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 6:00    [CountdownThisSection] 21:59    [CountdownFull] 35:59
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
FXWave command ignored because the current color world is Dark
TimeTrackerScript: [tick] 6:05    [CountdownThisSection] 21:54    [CountdownFull] 35:54
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue Sigh Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 6:10    [CountdownThisSection] 21:49    [CountdownFull] 35:49
TimeTrackerScript: [tick] 6:15    [CountdownThisSection] 21:44    [CountdownFull] 35:44
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue Mic OFF
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 6:20    [CountdownThisSection] 21:39    [CountdownFull] 35:39
TimeTrackerScript: [tick] 6:25    [CountdownThisSection] 21:34    [CountdownFull] 35:34
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 6:30    [CountdownThisSection] 21:29    [CountdownFull] 35:29
TimeTrackerScript: [tick] 6:35    [CountdownThisSection] 21:24    [CountdownFull] 35:24
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 6:40    [CountdownThisSection] 21:19    [CountdownFull] 35:19
TimeTrackerScript: [tick] 6:45    [CountdownThisSection] 21:14    [CountdownFull] 35:14
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 6:50    [CountdownThisSection] 21:08    [CountdownFull] 35:08
TimeTrackerScript: [tick] 6:55    [CountdownThisSection] 21:03    [CountdownFull] 35:03
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 7:00    [CountdownThisSection] 20:58    [CountdownFull] 34:58
TimeTrackerScript: [tick] 7:05    [CountdownThisSection] 20:53    [CountdownFull] 34:53
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 7:10    [CountdownThisSection] 20:48    [CountdownFull] 34:48
TimeTrackerScript: [tick] 7:15    [CountdownThisSection] 20:43    [CountdownFull] 34:43
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 7:20    [CountdownThisSection] 20:38    [CountdownFull] 34:38
TimeTrackerScript: [tick] 7:25    [CountdownThisSection] 20:33    [CountdownFull] 34:33
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 7:30    [CountdownThisSection] 20:28    [CountdownFull] 34:28
TimeTrackerScript: [tick] 7:35    [CountdownThisSection] 20:23    [CountdownFull] 34:23
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 7:40    [CountdownThisSection] 20:18    [CountdownFull] 34:18
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: PlayingSomaticSeq && Play_SoundSeedBreatheCycle
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Somatic Start
TimeTrackerScript: [tick] 7:45    [CountdownThisSection] 20:13    [CountdownFull] 34:13
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 7:50    [CountdownThisSection] 20:08    [CountdownFull] 34:08
TimeTrackerScript: [tick] 7:55    [CountdownThisSection] 20:03    [CountdownFull] 34:03
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 8:00    [CountdownThisSection] 19:58    [CountdownFull] 33:58
TimeTrackerScript: [tick] 8:05    [CountdownThisSection] 19:53    [CountdownFull] 33:53
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 8:10    [CountdownThisSection] 19:48    [CountdownFull] 33:48
TimeTrackerScript: [tick] 8:15    [CountdownThisSection] 19:43    [CountdownFull] 33:43
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 8:20    [CountdownThisSection] 19:38    [CountdownFull] 33:38
TimeTrackerScript: [tick] 8:25    [CountdownThisSection] 19:33    [CountdownFull] 33:33
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 8:30    [CountdownThisSection] 19:28    [CountdownFull] 33:28
TimeTrackerScript: [tick] 8:35    [CountdownThisSection] 19:22    [CountdownFull] 33:22
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 8:40    [CountdownThisSection] 19:17    [CountdownFull] 33:17
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
FXWave command ignored because the current color world is Dark
TimeTrackerScript: [tick] 8:45    [CountdownThisSection] 19:12    [CountdownFull] 33:12
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Unexpected Cue: AK_MusicSyncUserCue | Cue_BreathOut_Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 8:50    [CountdownThisSection] 19:07    [CountdownFull] 33:07
TimeTrackerScript: [tick] 8:55    [CountdownThisSection] 19:02    [CountdownFull] 33:02
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 9:00    [CountdownThisSection] 18:57    [CountdownFull] 32:57
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
FXWave command ignored because the current color world is Dark
TimeTrackerScript: [tick] 9:05    [CountdownThisSection] 18:52    [CountdownFull] 32:52
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Unexpected Cue: AK_MusicSyncUserCue | Cue_BreathOut_Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 9:10    [CountdownThisSection] 18:47    [CountdownFull] 32:47
TimeTrackerScript: [tick] 9:15    [CountdownThisSection] 18:42    [CountdownFull] 32:42
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
FXWave command ignored because the current color world is Dark
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 9:20    [CountdownThisSection] 18:37    [CountdownFull] 32:37
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Unexpected Cue: AK_MusicSyncUserCue | Cue_BreathOut_Start
TimeTrackerScript: [tick] 9:25    [CountdownThisSection] 18:32    [CountdownFull] 32:32
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 9:30    [CountdownThisSection] 18:27    [CountdownFull] 32:27
TimeTrackerScript: [tick] 9:35    [CountdownThisSection] 18:22    [CountdownFull] 32:22
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
FXWave command ignored because the current color world is Dark
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 9:40    [CountdownThisSection] 18:17    [CountdownFull] 32:17
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Unexpected Cue: AK_MusicSyncUserCue | Cue_BreathOut_Start
TimeTrackerScript: [tick] 9:45    [CountdownThisSection] 18:12    [CountdownFull] 32:12
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 9:50    [CountdownThisSection] 18:07    [CountdownFull] 32:07
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
FXWave command ignored because the current color world is Dark
TimeTrackerScript: [tick] 9:55    [CountdownThisSection] 18:02    [CountdownFull] 32:02
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Unexpected Cue: AK_MusicSyncUserCue | Cue_BreathOut_Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 10:00    [CountdownThisSection] 17:57    [CountdownFull] 31:57
TimeTrackerScript: [tick] 10:05    [CountdownThisSection] 17:52    [CountdownFull] 31:52
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 10:10    [CountdownThisSection] 17:47    [CountdownFull] 31:47
TimeTrackerScript: [tick] 10:15    [CountdownThisSection] 17:42    [CountdownFull] 31:42
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 10:20    [CountdownThisSection] 17:37    [CountdownFull] 31:37
TimeTrackerScript: [tick] 10:25    [CountdownThisSection] 17:31    [CountdownFull] 31:31
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 10:30    [CountdownThisSection] 17:26    [CountdownFull] 31:26
TimeTrackerScript: [tick] 10:36    [CountdownThisSection] 17:21    [CountdownFull] 31:21
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 10:41    [CountdownThisSection] 17:16    [CountdownFull] 31:16
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_InteractiveMusicSystem_Start
MUSIC: Set Silent Volume to 80
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
FXWave command ignored because the current color world is Dark
TimeTrackerScript: [tick] 10:46    [CountdownThisSection] 17:11    [CountdownFull] 31:11
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue Mic On
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_LinearHum_Start (expected is Cue_LinearHum_Start, variations allowed for backward compatibilty)
LightControl: Starting Lights with Delay
LightControl: Waiting for 1 second before starting lights
OpeningStageHandler: Making Wwise Tone
Sequencer: Triggering a False Tone in WWise
FirstVocalizationStart: Handled by sequence stages (current and/or transitioning-out).
LightControl: StartLights
Preferred color set to: Red
Transition to Red2 over 5 s with new currentColorType of Red
AVS: Starting Reference Signal
Binaural Beats: Lerping Binaural Beat Rate from 4 to 5.625
Strobe Rate set to: 45 Hz immediately
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
Director Change Detection REST: [ANCHOR SET] (60.14517)|(122.7421)
TimeTrackerScript: [tick] 10:51    [CountdownThisSection] 17:06    [CountdownFull] 31:06
Director Change Detection REST: unanchored with range threshold(0.065)
Director Queue: Director is disabled, not activating queue.
Director Change Detection: Breath Length Change
TimeTrackerScript: [tick] 10:56    [CountdownThisSection] 17:01    [CountdownFull] 31:01
Binaural Beats: New Rate is 5.625
Binaural Beats: Lerping Binaural Beat Rate from 5.625 to 5.5
Strobe Rate set to: 11 Hz over 30000 ms
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 11:01    [CountdownThisSection] 16:56    [CountdownFull] 30:56
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
AVS FXWave started with key: 0
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_LinearHum_Start (expected is Cue_LinearHum_Start, variations allowed for backward compatibilty)
LightControl: Starting Lights with Delay
LightControl: Waiting for 1 second before starting lights
OpeningStageHandler: Making Wwise Tone
Sequencer: Triggering a False Tone in WWise
FirstVocalizationStart: Handled by sequence stages (current and/or transitioning-out).
LightControl: StartLights already initialized, skipping
TimeTrackerScript: [tick] 11:06    [CountdownThisSection] 16:51    [CountdownFull] 30:51
Binaural Beats: New Rate is 5.5
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
Sequencer: Stopping a False Tone in WWise
TimeTrackerScript: [tick] 11:11    [CountdownThisSection] 16:46    [CountdownFull] 30:46
TimeTrackerScript: [tick] 11:16    [CountdownThisSection] 16:41    [CountdownFull] 30:41
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 11:21    [CountdownThisSection] 16:36    [CountdownFull] 30:36
TimeTrackerScript: [tick] 11:26    [CountdownThisSection] 16:31    [CountdownFull] 30:31
Binaural Beats: Lerping Binaural Beat Rate from 5.5 to 4.25
Strobe Rate set to: 8.5 Hz over 150000 ms
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=1, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 11:31    [CountdownThisSection] 16:26    [CountdownFull] 30:26
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
AVS FXWave started with key: 1
TimeTrackerScript: [tick] 11:36    [CountdownThisSection] 16:21    [CountdownFull] 30:21
Binaural Beats: New Rate is 4.25
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_StartTutorial (expected is Cue_Tutorial_Start, variations allowed for backward compatibilty)
Sequencer: IsOpeningMusicPlaying true → false (NotifyOpeningMusicEnded). Invoking OnOpeningMusicEnded.
OpeningStageHandler: Marking stage complete. Note that audio may still be playing from this stage.
StartTutorial: Handled by sequence stages (current and/or transitioning-out).
OpeningStageHandler: BeginTransitionOut.
TutorialStageHandler: Enter
Tutorial: SetTestVocalizationType: Hum
Tutorial: StartTutorial: Long
Tutorial: Voice Test Coroutine
Tutorial: About to test...
Tutorial: START
LightControl: StartLights already initialized, skipping
MUSIC: Setting Music Mode to InteractiveTutorial...
Binaural Beats: Lerping Volume from 0 to 70over 5 seconds
MUSIC: Music Mode Set to Tutorial
MUSIC: InteractiveMusic started
MUSIC 6: Fundamental Note Changing to C
Director Queue: Removed all fundamentalChange items from director queue.
Binaural Beats: Fading Out Binaural Beats with Stop event for frequency change to 261.6255 Hz
MUSIC 8: Key(C: ChangeFundamentalTimer reset
MUSIC 8: Key(Cs: ChangeFundamentalTimer reset
MUSIC 8: Key(D: ChangeFundamentalTimer reset
MUSIC 8: Key(Ds: ChangeFundamentalTimer reset
MUSIC 8: Key(E: ChangeFundamentalTimer reset
MUSIC 8: Key(F: ChangeFundamentalTimer reset
MUSIC 8: Key(Fs: ChangeFundamentalTimer reset
MUSIC 8: Key(G: ChangeFundamentalTimer reset
MUSIC 8: Key(Gs: ChangeFundamentalTimer reset
MUSIC 8: Key(A: ChangeFundamentalTimer reset
MUSIC 8: Key(As: ChangeFundamentalTimer reset
MUSIC 8: Key(B: ChangeFundamentalTimer reset
MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Locked to C
MUSIC: Interactive Music Mode Recovered to InteractiveMusicSystem because Interaction Type is SoundWorld
MUSIC: Set Silent Volume to 80
Sequence: Entered stage 4 (Tutorial)
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_LinearHum_Start (expected is Cue_LinearHum_Start, variations allowed for backward compatibilty)
LightControl: Starting Lights with Delay
LightControl: Waiting for 1 second before starting lights
OpeningStageHandler: Making Wwise Tone
Sequencer: Triggering a False Tone in WWise
FirstVocalizationStart: Handled by sequence stages (current and/or transitioning-out).
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Unexpected Cue: AK_MusicSyncUserCue | Cue_AVS_Start
LightControl: StartLights already initialized, skipping
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
MUSIC: Harmony Note Set To: F
MUSIC: Harmony Played: F ~ (fundamentalNoteName + 5)
Tutorial: Testing...
Tutorial: TEST SUCCESS (wait for breath)
TimeTrackerScript: [tick] 11:41    [CountdownThisSection] 16:16    [CountdownFull] 30:16
Binaural Beats: Changing Center Frequency to 261.6255 Hz, and fading in again with Play event
Binaural Beats: New Volume is 70
AVS FXWave started with key: 2
TimeTrackerScript: [tick] 11:46    [CountdownThisSection] 16:11    [CountdownFull] 30:11
Tutorial: (Long) Played Hum guidance, guidanceCount: 1
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 11:51    [CountdownThisSection] 16:06    [CountdownFull] 30:06
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
AVS FXWave started with key: 3
TimeTrackerScript: [tick] 11:56    [CountdownThisSection] 16:01    [CountdownFull] 30:01
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
MUSIC: Harmony Note Set To: G
MUSIC: Harmony Played: G ~ (fundamentalNoteName + 7)
TimeTrackerScript: [tick] 12:01    [CountdownThisSection] 15:56    [CountdownFull] 29:56
Tutorial: TEST SUCCESS (wait for breath)
Tutorial: (Long) Played Hum guidance, guidanceCount: 2
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
TimeTrackerScript: [tick] 12:06    [CountdownThisSection] 15:51    [CountdownFull] 29:51
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
AVS FXWave started with key: 4
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
MUSIC: Harmony Note Set To: C
MUSIC: Harmony Played: C ~ (fundamentalNoteName + 12)
MUSIC: Harmony Note Set To: F
MUSIC: Harmony Played: F ~ (fundamentalNoteName + 5)
TimeTrackerScript: [tick] 12:11    [CountdownThisSection] 15:46    [CountdownFull] 29:45
MUSIC: Harmony Note Set To: G
MUSIC: Harmony Played: G ~ (fundamentalNoteName + 7)
Tutorial: TEST SUCCESS (wait for breath)
Director Queue: Director is disabled, not activating queue.
Tutorial: (Long) Played Hum guidance, guidanceCount: 3
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
TimeTrackerScript: [tick] 12:16    [CountdownThisSection] 15:41    [CountdownFull] 29:40
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 12:21    [CountdownThisSection] 15:36    [CountdownFull] 29:35
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
AVS FXWave started with key: 5
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
TimeTrackerScript: [tick] 12:26    [CountdownThisSection] 15:31    [CountdownFull] 29:30
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
MUSIC: Harmony Note Set To: C
MUSIC: Harmony Played: C ~ (fundamentalNoteName + 12)
TimeTrackerScript: [tick] 12:31    [CountdownThisSection] 15:26    [CountdownFull] 29:25
Tutorial: TEST SUCCESS (wait for breath)
Tutorial: (Long) Played Hum guidance, guidanceCount: 4
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
TimeTrackerScript: [tick] 12:36    [CountdownThisSection] 15:21    [CountdownFull] 29:20
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
AVS FXWave started with key: 6
TimeTrackerScript: [tick] 12:41    [CountdownThisSection] 15:16    [CountdownFull] 29:15
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
TimeTrackerScript: [tick] 12:46    [CountdownThisSection] 15:11    [CountdownFull] 29:10
MUSIC: Harmony Note Set To: F
MUSIC: Harmony Played: F ~ (fundamentalNoteName + 5)
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
Tutorial: TEST SUCCESS (wait for breath)
TimeTrackerScript: [tick] 12:51    [CountdownThisSection] 15:06    [CountdownFull] 29:05
AVS FXWave started with key: 7
Tutorial: (Long) Played Hum guidance, guidanceCount: 5
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
TimeTrackerScript: [tick] 12:56    [CountdownThisSection] 15:01    [CountdownFull] 29:00
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 13:01    [CountdownThisSection] 14:56    [CountdownFull] 28:55
TimeTrackerScript: [tick] 13:06    [CountdownThisSection] 14:51    [CountdownFull] 28:50
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
AVS FXWave started with key: 8
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_ChangeVocalizationTypeFromHmmToAhh — Long vocalization type and side effects are now driven by tutorial guidanceCount; cue kept for Wwise timeline compatibility.
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 13:11    [CountdownThisSection] 14:46    [CountdownFull] 28:45
MUSIC: Harmony Note Set To: G
MUSIC: Harmony Played: G ~ (fundamentalNoteName + 7)
Director Change Detection REST: [ANCHOR SET] (13.34451)|(16.42998)
Director Queue: Director is disabled, not activating queue.
Tutorial: TEST SUCCESS (wait for breath)
TimeTrackerScript: [tick] 13:16    [CountdownThisSection] 14:41    [CountdownFull] 28:40
MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Unlocked
Tutorial: (Long) guidanceCount 5 → vocalization type Ahh (was Hum)
Tutorial: (Long) Played Ahh guidance, guidanceCount: 6
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 13:21    [CountdownThisSection] 14:36    [CountdownFull] 28:35
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
TimeTrackerScript: [tick] 13:26    [CountdownThisSection] 14:31    [CountdownFull] 28:30
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 13:31    [CountdownThisSection] 14:26    [CountdownFull] 28:25
Tutorial: TEST FAIL
Tutorial: Provide Correction, playing guidance...
MUSIC 6: Fundamental Note Changing to C
Director Queue: Removed all fundamentalChange items from director queue.
MUSIC 8: Key(C: ChangeFundamentalTimer reset
MUSIC 8: Key(Cs: ChangeFundamentalTimer reset
MUSIC 8: Key(D: ChangeFundamentalTimer reset
MUSIC 8: Key(Ds: ChangeFundamentalTimer reset
MUSIC 8: Key(E: ChangeFundamentalTimer reset
MUSIC 8: Key(F: ChangeFundamentalTimer reset
MUSIC 8: Key(Fs: ChangeFundamentalTimer reset
MUSIC 8: Key(G: ChangeFundamentalTimer reset
MUSIC 8: Key(Gs: ChangeFundamentalTimer reset
MUSIC 8: Key(A: ChangeFundamentalTimer reset
MUSIC 8: Key(As: ChangeFundamentalTimer reset
MUSIC 8: Key(B: ChangeFundamentalTimer reset
MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Locked to C
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
TimeTrackerScript: [tick] 13:36    [CountdownThisSection] 14:21    [CountdownFull] 28:20
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Unexpected Cue: AK_MusicSyncUserCue | Cue_Breathin_Start
TimeTrackerScript: [tick] 13:41    [CountdownThisSection] 14:16    [CountdownFull] 28:15
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing correction...
MUSIC: Harmony Note Set To: C
MUSIC: Harmony Played: C ~ (fundamentalNoteName + 12)
Director Change Detection REST: unanchored with range threshold(0.065)
Director Queue: Director is disabled, not activating queue.
Director Change Detection: Breath Length Change
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_LinearHum_Start (expected is Cue_LinearHum_Start, variations allowed for backward compatibilty)
LightControl: Starting Lights with Delay
LightControl: Waiting for 1 second before starting lights
OpeningStageHandler: Making Wwise Tone
Sequencer: Triggering a False Tone in WWise
FirstVocalizationStart: Handled by sequence stages (current and/or transitioning-out).
Tutorial: CORRECTION TEST SUCCESS (wait for breath...)
TimeTrackerScript: [tick] 13:46    [CountdownThisSection] 14:11    [CountdownFull] 28:10
LightControl: StartLights already initialized, skipping
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 13:51    [CountdownThisSection] 14:06    [CountdownFull] 28:05
AVS FXWave started with key: 9
Tutorial: Play correction confirmation vo
MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Unlocked
Director Queue: Removed all fundamentalChange items from director queue.
Director Queue: Director is disabled, not adding action to queue.
MUSIC FUNDAMENNTAL MODE UNLOCK: New Fundamental Queued on Unlock: G
WWise_VO: Play Repair Success
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
TimeTrackerScript: [tick] 13:56    [CountdownThisSection] 14:01    [CountdownFull] 28:00
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
AVS FXWave started with key: 10
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
Director Queue: Director is disabled, not adding action to queue.
Strobe Rate set to: 11.5 Hz over 180000 ms
Binaural Beats: Lerping Binaural Beat Rate from 4.25 to 5.75
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
MUSIC: Harmony Note Set To: F
MUSIC: Harmony Played: F ~ (fundamentalNoteName + 5)
Tutorial: TEST SUCCESS (wait for breath)
TimeTrackerScript: [tick] 14:01    [CountdownThisSection] 13:56    [CountdownFull] 27:54
AVS FXWave started with key: 11
TimeTrackerScript: [tick] 14:06    [CountdownThisSection] 13:51    [CountdownFull] 27:49
Tutorial: (Long) Played Ahh guidance, guidanceCount: 7
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
MUSIC: Harmony Note Set To: G
MUSIC: Harmony Played: G ~ (fundamentalNoteName + 7)
TimeTrackerScript: [tick] 14:11    [CountdownThisSection] 13:46    [CountdownFull] 27:44
Tutorial: TEST SUCCESS (wait for breath)
MUSIC: Long Test Instantly Triggering Fundamental Change to G
MUSIC 6: Fundamental Note Changing to G
Director Queue: Removed all fundamentalChange items from director queue.
Binaural Beats: Fading Out Binaural Beats with Stop event for frequency change to 195.9977 Hz
MUSIC 8: Key(C: ChangeFundamentalTimer reset
MUSIC 8: Key(Cs: ChangeFundamentalTimer reset
MUSIC 8: Key(D: ChangeFundamentalTimer reset
MUSIC 8: Key(Ds: ChangeFundamentalTimer reset
MUSIC 8: Key(E: ChangeFundamentalTimer reset
MUSIC 8: Key(F: ChangeFundamentalTimer reset
MUSIC 8: Key(Fs: ChangeFundamentalTimer reset
MUSIC 8: Key(G: ChangeFundamentalTimer reset
MUSIC 8: Key(Gs: ChangeFundamentalTimer reset
MUSIC 8: Key(A: ChangeFundamentalTimer reset
MUSIC 8: Key(As: ChangeFundamentalTimer reset
MUSIC 8: Key(B: ChangeFundamentalTimer reset
Director Queue: Director is disabled, not activating queue.
TimeTrackerScript: [tick] 14:16    [CountdownThisSection] 13:41    [CountdownFull] 27:39
Tutorial: (Long) Played Ahh guidance, guidanceCount: 8
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
Binaural Beats: Changing Center Frequency to 195.9977 Hz, and fading in again with Play event
TimeTrackerScript: [tick] 14:21    [CountdownThisSection] 13:36    [CountdownFull] 27:34
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
AVS FXWave started with key: 12
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
MUSIC: Harmony Note Set To: G
MUSIC: Harmony Played: G ~ (fundamentalNoteName + 12)
TimeTrackerScript: [tick] 14:26    [CountdownThisSection] 13:31    [CountdownFull] 27:29
Tutorial: TEST SUCCESS (wait for breath)
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
Tutorial: (Long) Played Ahh guidance, guidanceCount: 9
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
TimeTrackerScript: [tick] 14:31    [CountdownThisSection] 13:26    [CountdownFull] 27:24
TimeTrackerScript: [tick] 14:36    [CountdownThisSection] 13:21    [CountdownFull] 27:19
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 14:41    [CountdownThisSection] 13:16    [CountdownFull] 27:14
Director Queue: Director is disabled, not adding action to queue.
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
AVS FXWave started with key: 13
TimeTrackerScript: [tick] 14:46    [CountdownThisSection] 13:11    [CountdownFull] 27:09
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
MUSIC: Harmony Note Set To: C
MUSIC: Harmony Played: C ~ (fundamentalNoteName + 5)
Tutorial: TEST SUCCESS (wait for breath)
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 14:51    [CountdownThisSection] 13:06    [CountdownFull] 27:04
AVS FXWave started with key: 14
Tutorial: (Long) Played Ahh guidance, guidanceCount: 10
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
TimeTrackerScript: [tick] 14:56    [CountdownThisSection] 13:01    [CountdownFull] 26:59
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 15:01    [CountdownThisSection] 12:56    [CountdownFull] 26:54
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_BreathIn
AVS FXWave started with key: 15
TimeTrackerScript: [tick] 15:06    [CountdownThisSection] 12:51    [CountdownFull] 26:49
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_ChangeVocalizationTypeFromAhhToOhh — Long vocalization type and side effects are now driven by tutorial guidanceCount; cue kept for Wwise timeline compatibility.
MUSIC: Harmony Note Set To: G
MUSIC: Harmony Played: G ~ (fundamentalNoteName + 12)
Tutorial: TEST SUCCESS (wait for breath)
TimeTrackerScript: [tick] 15:11    [CountdownThisSection] 12:46    [CountdownFull] 26:44
Director Queue: Director is disabled, not activating queue.
TimeTrackerScript: [tick] 15:16    [CountdownThisSection] 12:41    [CountdownFull] 26:39
AVS FXWave started with key: 16
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
MUSIC FUNDAMENTAL-MODE-LOCK: Tried to unlock fundamental mode, but it was already unlocked
Tutorial: (Long) guidanceCount 10 → vocalization type Ohh (was Ahh)
Tutorial: (Long) Played Ohh guidance, guidanceCount: 11
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
TimeTrackerScript: [tick] 15:21    [CountdownThisSection] 12:36    [CountdownFull] 26:34
TimeTrackerScript: [tick] 15:26    [CountdownThisSection] 12:31    [CountdownFull] 26:29
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
AVS FXWave started with key: 17
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
TimeTrackerScript: [tick] 15:31    [CountdownThisSection] 12:26    [CountdownFull] 26:24
MUSIC: Harmony Note Set To: C
MUSIC: Harmony Played: C ~ (fundamentalNoteName + 5)
Director Change Detection REST: [ANCHOR SET] (15.28582)|(16.83141)
TimeTrackerScript: [tick] 15:36    [CountdownThisSection] 12:21    [CountdownFull] 26:19
Tutorial: TEST SUCCESS (wait for breath)
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 15:41    [CountdownThisSection] 12:16    [CountdownFull] 26:14
AVS FXWave started with key: 18
Director Change Detection TONE: [ANCHOR SET] (8.109556)|(9.510241)
Tutorial: (Long) Played Ohh guidance, guidanceCount: 12
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
TimeTrackerScript: [tick] 15:46    [CountdownThisSection] 12:11    [CountdownFull] 26:08
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 15:51    [CountdownThisSection] 12:06    [CountdownFull] 26:03
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
AVS FXWave started with key: 19
TimeTrackerScript: [tick] 15:56    [CountdownThisSection] 12:01    [CountdownFull] 25:58
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
MUSIC: Harmony Note Set To: G
MUSIC: Harmony Played: G ~ (fundamentalNoteName + 12)
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
Director Change Detection REST: unanchored with range threshold(0.065)
Director Queue: Director is disabled, not activating queue.
Director Change Detection: Breath Length Change
Tutorial: TEST SUCCESS (wait for breath)
TimeTrackerScript: [tick] 16:01    [CountdownThisSection] 11:56    [CountdownFull] 25:53
AVS FXWave started with key: 20
TimeTrackerScript: [tick] 16:06    [CountdownThisSection] 11:51    [CountdownFull] 25:48
Tutorial: (Long) Played Ohh guidance, guidanceCount: 13
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 16:11    [CountdownThisSection] 11:46    [CountdownFull] 25:43
TimeTrackerScript: [tick] 16:16    [CountdownThisSection] 11:41    [CountdownFull] 25:38
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 16:21    [CountdownThisSection] 11:36    [CountdownFull] 25:33
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_BreathIn
AVS FXWave started with key: 21
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
MUSIC: Harmony Note Set To: C
MUSIC: Harmony Played: C ~ (fundamentalNoteName + 5)
TimeTrackerScript: [tick] 16:26    [CountdownThisSection] 11:31    [CountdownFull] 25:28
Tutorial: TEST SUCCESS (wait for breath)
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 16:31    [CountdownThisSection] 11:26    [CountdownFull] 25:23
AVS FXWave started with key: 22
Director Queue: Director is disabled, not activating queue.
Director Change Detection: Tone Length Change
Director Change Detection TONE: unanchored with range threshold(0.1)
Tutorial: (Long) Played Ohh guidance, guidanceCount: 14
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
TimeTrackerScript: [tick] 16:36    [CountdownThisSection] 11:21    [CountdownFull] 25:18
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 16:41    [CountdownThisSection] 11:16    [CountdownFull] 25:13
TimeTrackerScript: [tick] 16:46    [CountdownThisSection] 11:11    [CountdownFull] 25:08
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_FreePlay
MUSIC: Set Silent Volume to 80
Director Queue: Director Enabled
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
AVS FXWave started with key: 23
TimeTrackerScript: [tick] 16:51    [CountdownThisSection] 11:06    [CountdownFull] 25:03
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_ChangeVocalizationTypeFromOhhToAdvanced — Long vocalization type and side effects are now driven by tutorial guidanceCount; cue kept for Wwise timeline compatibility.
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
MUSIC: Harmony Note Set To: G
MUSIC: Harmony Played: G ~ (fundamentalNoteName + 12)
Director Change Detection REST: [ANCHOR SET] (16.14295)|(17.07322)
Tutorial: TEST SUCCESS (wait for breath)
TimeTrackerScript: [tick] 16:56    [CountdownThisSection] 11:01    [CountdownFull] 24:58
Director Queue: Removed all monostereo items from director queue.
Director Queue: Added 0 monostereo to director queue.
Binaural Beats: New Rate is 5.75
Strobe Rate set to: 8.5 Hz over 180000 ms
Binaural Beats: Lerping Binaural Beat Rate from 5.75 to 4.25
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
AVS FXWave started with key: 24
MUSIC FUNDAMENTAL-MODE-LOCK: Tried to unlock fundamental mode, but it was already unlocked
WorldShuffler: Beginning shuffle immediately.
MUSIC FUNDAMENTAL-CONTENT-LOCK: Tried to unlock fundamental content, but it was already unlocked
MUSIC: Soundscape Set To: Shruti (SoundWorld)
MUSIC TEST: currentInteractionType after SetSoundscape: SoundWorld
Director: Transition Sound Played
WorldShuffler: Starting shuffle timer.
Preferred color set to: White
Transition to White2 over 2 s with new currentColorType of White
WorldShuffler: Starting shuffle timer.
WorldShuffler: Starting shuffle timer.
Tutorial: (Long) guidanceCount 14 → vocalization type Advanced (was Ohh)
Tutorial: (Long) Played Advanced guidance, guidanceCount: 15
Tutorial: Voice Test Coroutine
Tutorial: About to test...
MUSIC: Set Silent Volume to 80
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
TimeTrackerScript: [tick] 17:01    [CountdownThisSection] 10:56    [CountdownFull] 24:53
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
Director: Transition Sound cooldown expired - can play again.
TimeTrackerScript: [tick] 17:06    [CountdownThisSection] 10:51    [CountdownFull] 24:48
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
MUSIC: Harmony Note Set To: C
MUSIC: Harmony Played: C ~ (fundamentalNoteName + 5)
MUSIC: Harmony Note Set To: G
MUSIC: Harmony Played: G ~ (fundamentalNoteName + 12)
Director Change Detection REST: unanchored with range threshold(0.065)
AVS: Switching to Stereo
Director Queue: Action monostereo executed from process-all
Director Queue: No Audio Actions Queued, triggering one to complete syncresis
Director: Audio Tweak to 0 in 5000ms (this isn't in wwise yet, I think)
Director: Transition Sound Played
Director Change Detection: Breath Length Change
Tutorial: TEST SUCCESS (wait for breath)
TimeTrackerScript: [tick] 17:11    [CountdownThisSection] 10:46    [CountdownFull] 24:43
Director: Transition Sound cooldown expired - can play again.
Tutorial: (Long) Played Advanced guidance, guidanceCount: 16
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
TimeTrackerScript: [tick] 17:16    [CountdownThisSection] 10:41    [CountdownFull] 24:38
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
TimeTrackerScript: [tick] 17:21    [CountdownThisSection] 10:36    [CountdownFull] 24:33
TimeTrackerScript: [tick] 17:26    [CountdownThisSection] 10:31    [CountdownFull] 24:28
Tutorial: TEST FAIL
Tutorial: Provide Correction, playing guidance...
MUSIC 6: Fundamental Note Changing to C
Director Queue: Removed all fundamentalChange items from director queue.
Binaural Beats: Fading Out Binaural Beats with Stop event for frequency change to 261.6255 Hz
MUSIC 8: Key(C: ChangeFundamentalTimer reset
MUSIC 8: Key(Cs: ChangeFundamentalTimer reset
MUSIC 8: Key(D: ChangeFundamentalTimer reset
MUSIC 8: Key(Ds: ChangeFundamentalTimer reset
MUSIC 8: Key(E: ChangeFundamentalTimer reset
MUSIC 8: Key(F: ChangeFundamentalTimer reset
MUSIC 8: Key(Fs: ChangeFundamentalTimer reset
MUSIC 8: Key(G: ChangeFundamentalTimer reset
MUSIC 8: Key(Gs: ChangeFundamentalTimer reset
MUSIC 8: Key(A: ChangeFundamentalTimer reset
MUSIC 8: Key(As: ChangeFundamentalTimer reset
MUSIC 8: Key(B: ChangeFundamentalTimer reset
MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Locked to C
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
Binaural Beats: Changing Center Frequency to 261.6255 Hz, and fading in again with Play event
TimeTrackerScript: [tick] 17:31    [CountdownThisSection] 10:26    [CountdownFull] 24:23
TimeTrackerScript: [tick] 17:36    [CountdownThisSection] 10:21    [CountdownFull] 24:18
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Unexpected Cue: AK_MusicSyncUserCue | Cue_Breathin_Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 17:41    [CountdownThisSection] 10:16    [CountdownFull] 24:13
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_LinearHum_Start (expected is Cue_LinearHum_Start, variations allowed for backward compatibilty)
LightControl: Starting Lights with Delay
LightControl: Waiting for 1 second before starting lights
OpeningStageHandler: Making Wwise Tone
Sequencer: Triggering a False Tone in WWise
FirstVocalizationStart: Handled by sequence stages (current and/or transitioning-out).
Tutorial: Testing correction...
LightControl: StartLights already initialized, skipping
Director Queue: Removed all monostereo items from director queue.
Director Queue: Added 1 monostereo to director queue.
MUSIC: Harmony Note Set To: F
MUSIC: Harmony Played: F ~ (fundamentalNoteName + 5)
Tutorial: CORRECTION TEST SUCCESS (wait for breath...)
TimeTrackerScript: [tick] 17:46    [CountdownThisSection] 10:11    [CountdownFull] 24:08
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
Tutorial: Play correction confirmation vo
MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Unlocked
WWise_VO: Play Repair Success
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
TimeTrackerScript: [tick] 17:51    [CountdownThisSection] 10:06    [CountdownFull] 24:03
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue BreathIn Start
AVS FXWave started with key: 25
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
TimeTrackerScript: [tick] 17:56    [CountdownThisSection] 10:01    [CountdownFull] 23:58
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
MUSIC: Harmony Note Set To: G
MUSIC: Harmony Played: G ~ (fundamentalNoteName + 7)
TimeTrackerScript: [tick] 18:01    [CountdownThisSection] 9:56    [CountdownFull] 23:53
WorldShuffler: Time to shuffle worlds.
WorldShuffler: Queuing World Shuffle
Director Queue: Added 2 SoundscapeShuffle to director queue.
Director Queue: Added 3 ColorWorldShuffle to director queue.
WorldShuffler: Waiting for next shuffle to begin timer.
Tutorial: TEST SUCCESS (wait for breath)
TimeTrackerScript: [tick] 18:06    [CountdownThisSection] 9:51    [CountdownFull] 23:48
AVS FXWave started with key: 26
Director Change Detection TONE: [ANCHOR SET] (6.347613)|(7.018692)
Tutorial: (Long) Played Advanced guidance, guidanceCount: 17
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
TimeTrackerScript: [tick] 18:11    [CountdownThisSection] 9:46    [CountdownFull] 23:43
MUSIC: Harmony Note Set To: C
MUSIC: Harmony Played: C ~ (fundamentalNoteName + 12)
Tutorial: TEST SUCCESS (wait for breath)
TimeTrackerScript: [tick] 18:16    [CountdownThisSection] 9:42    [CountdownFull] 23:38
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
AVS FXWave started with key: 27
TimeTrackerScript: [tick] 18:21    [CountdownThisSection] 9:37    [CountdownFull] 23:33
MUSIC FUNDAMENTAL-CONTENT-LOCK: Tried to unlock fundamental content, but it was already unlocked
MUSIC: Soundscape Set To: Gentle (SoundWorld)
MUSIC TEST: currentInteractionType after SetSoundscape: SoundWorld
Director: Transition Sound Played
WorldShuffler: Starting shuffle timer.
Director Queue: Action SoundscapeShuffle executed from process-all
Preferred color set to: Red
Transition to Red2 over 2 s with new currentColorType of Red
WorldShuffler: Starting shuffle timer.
Director Queue: Action ColorWorldShuffle executed from process-all
AVS: Switching to Mono
Director Queue: Action monostereo executed from process-all
Director Change Detection: Tone Length Change
Director Change Detection TONE: unanchored with range threshold(0.1)
Tutorial: (Long) Played Advanced guidance, guidanceCount: 18
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Unexpected Cue: AK_MusicSyncUserCue | Cue_BreakAllTests
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
Tutorial: Testing...
TimeTrackerScript: [tick] 18:26    [CountdownThisSection] 9:32    [CountdownFull] 23:28
Director: Transition Sound cooldown expired - can play again.
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 18:31    [CountdownThisSection] 9:27    [CountdownFull] 23:23
MUSIC: Harmony Note Set To: F
MUSIC: Harmony Played: F ~ (fundamentalNoteName + 5)
Director Change Detection REST: [ANCHOR SET] (7.964744)|(10.00255)
Director Queue: ActivateQueue called but queue is empty
Tutorial: TEST SUCCESS (wait for breath)
Director Queue: Removed all fundamentalChange items from director queue.
Director Queue: Removed all fundamentalChange items from director queue.
Director Queue: Added 4 fundamentalChange to director queue.
MUSIC: Short Test New Fundamental Queued: A
TimeTrackerScript: [tick] 18:36    [CountdownThisSection] 9:22    [CountdownFull] 23:18
Tutorial: (Long) Played Advanced guidance, guidanceCount: 19
Tutorial: Voice Test Coroutine
Tutorial: About to test...
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=2, switch=0) hardSteps=0 source=Normalized enabled=True
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Wwise_Tutorial_Break_All_Tests
TutorialStageHandler: Marking stage complete.
Break_Tests: Handled by sequence stages (current and/or transitioning-out).
Tutorial: Stopping
TutorialPassed: Handled by sequence stages (current and/or transitioning-out).
WWise_VO: Stop Opening Sequence
MUSIC: Setting Music Mode to Freeplay...
Binaural Beats: Lerping Volume from 70 to 70over 5 seconds
MUSIC: Music Mode Set to Freeplay
MUSIC FUNDAMENTAL-MODE-LOCK: Tried to unlock fundamental mode, but it was already unlocked
MUSIC: InteractiveMusic is already started
MUSIC: Set Silent Volume to 80
MUSIC: Interactive Music Mode Recovered to InteractiveMusicSystem because Interaction Type is SoundWorld
PlaygroundStageHandler: Standard variant: Starting Standard Playground coroutine.
Sequence: Entered stage 5 (Playground)
WWise_VO_CUE: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue_VO_GuidedVocalization_End
TimeTrackerScript: [tick] 18:41    [CountdownThisSection] 9:17    [CountdownFull] 23:13
MUSIC: Harmony Note Set To: G
MUSIC: Harmony Played: G ~ (fundamentalNoteName + 7)
Director Change Detection REST: unanchored with range threshold(0.065)
MUSIC 6: Fundamental Note Changing to A
Director Queue: Removed all fundamentalChange items from director queue.
Binaural Beats: Fading Out Binaural Beats with Stop event for frequency change to 220 Hz
MUSIC 8: Key(C: ChangeFundamentalTimer reset
MUSIC 8: Key(Cs: ChangeFundamentalTimer reset
MUSIC 8: Key(D: ChangeFundamentalTimer reset
MUSIC 8: Key(Ds: ChangeFundamentalTimer reset
MUSIC 8: Key(E: ChangeFundamentalTimer reset
MUSIC 8: Key(F: ChangeFundamentalTimer reset
MUSIC 8: Key(Fs: ChangeFundamentalTimer reset
MUSIC 8: Key(G: ChangeFundamentalTimer reset
MUSIC 8: Key(Gs: ChangeFundamentalTimer reset
MUSIC 8: Key(A: ChangeFundamentalTimer reset
MUSIC 8: Key(As: ChangeFundamentalTimer reset
MUSIC 8: Key(B: ChangeFundamentalTimer reset
Director Queue: Action fundamentalChange executed from process-all
Director Queue: No Visual Actions Queued, Triggering one to complete syncresis
Transition to Red1 over 5 s with new currentColorType of Red
AVS FXWave started with key: 28
Director Change Detection: Breath Length Change
Binaural Beats: New Volume is 70
TimeTrackerScript: [tick] 18:46    [CountdownThisSection] 9:12    [CountdownFull] 23:08
Binaural Beats: Changing Center Frequency to 220 Hz, and fading in again with Play event
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
AVS FXWave started with key: 29
[MicVoiceIngest] FAIL_OBSERVATION composite is now TRUE — flags: FAIL_AUDIO_CALLBACK_GAP_HIGH | micExitReason=unread_zero hzRolling=46.7 maxGapMs=42.71 imitone/callback=0.999 rawLockMisses=62 overflowDrops=0 dbMicSnap=-66.08 imitoneStateStallFrames=96
[MicVoiceIngest] FAIL_OBSERVATION still TRUE after 0.26s — flags: FAIL_AUDIO_CALLBACK_GAP_HIGH | micExitReason=unread_zero hzRolling=46.7 maxGapMs=42.71 imitone/callback=0.999 rawLockMisses=62 overflowDrops=0 dbMicSnap=-68.45 imitoneStateStallFrames=138
[MicVoiceIngest] FAIL_OBSERVATION still TRUE after 0.76s — flags: FAIL_AUDIO_CALLBACK_GAP_HIGH | micExitReason=copied_samples hzRolling=46.7 maxGapMs=42.71 imitone/callback=0.999 rawLockMisses=62 overflowDrops=0 dbMicSnap=-67.81 imitoneStateStallFrames=221
[MicVoiceIngest] FAIL_OBSERVATION cleared after 1.01s — flags seen during window: FAIL_AUDIO_CALLBACK_GAP_HIGH 
TimeTrackerScript: [tick] 18:51    [CountdownThisSection] 9:07    [CountdownFull] 23:03
MUSIC: Harmony Note Set To: A
MUSIC: Harmony Played: A ~ (fundamentalNoteName + 12)
TimeTrackerScript: [tick] 18:56    [CountdownThisSection] 9:02    [CountdownFull] 22:58
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
AVS FXWave started with key: 30
TimeTrackerScript: [tick] 19:01    [CountdownThisSection] 8:57    [CountdownFull] 22:53
MUSIC: Harmony Note Set To: D
MUSIC: Harmony Played: D ~ (fundamentalNoteName + 5)
TimeTrackerScript: [tick] 19:06    [CountdownThisSection] 8:52    [CountdownFull] 22:48
MUSIC: Harmony Note Set To: E
MUSIC: Harmony Played: E ~ (fundamentalNoteName + 7)
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 19:11    [CountdownThisSection] 8:47    [CountdownFull] 22:43
MUSIC: Harmony Note Set To: A
MUSIC: Harmony Played: A ~ (fundamentalNoteName + 12)
TimeTrackerScript: [tick] 19:16    [CountdownThisSection] 8:42    [CountdownFull] 22:38
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 19:21    [CountdownThisSection] 8:37    [CountdownFull] 22:33
WorldShuffler: Time to shuffle worlds.
WorldShuffler: Queuing World Shuffle
Director Queue: Added 5 SoundscapeShuffle to director queue.
Director Queue: Added 6 ColorWorldShuffle to director queue.
WorldShuffler: Waiting for next shuffle to begin timer.
TimeTrackerScript: [tick] 19:26    [CountdownThisSection] 8:32    [CountdownFull] 22:28
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 19:31    [CountdownThisSection] 8:27    [CountdownFull] 22:23
TimeTrackerScript: [tick] 19:36    [CountdownThisSection] 8:22    [CountdownFull] 22:18
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
WorldShuffler: Resetting all soundscapes to be available for shuffling.
PlaygroundStageHandler(Standard): Start1 at 60s — reset soundscape exclusions.
TimeTrackerScript: [tick] 19:41    [CountdownThisSection] 8:17    [CountdownFull] 22:13
TimeTrackerScript: [tick] 19:46    [CountdownThisSection] 8:12    [CountdownFull] 22:08
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 19:51    [CountdownThisSection] 8:07    [CountdownFull] 22:03
TimeTrackerScript: [tick] 19:56    [CountdownThisSection] 8:02    [CountdownFull] 21:58
Director Queue: Removed all monostereo items from director queue.
Director Queue: Added 7 monostereo to director queue.
Binaural Beats: New Rate is 4.25
Strobe Rate set to: 11.5 Hz over 180000 ms
Binaural Beats: Lerping Binaural Beat Rate from 4.25 to 5.75
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 20:01    [CountdownThisSection] 7:57    [CountdownFull] 21:53
TimeTrackerScript: [tick] 20:06    [CountdownThisSection] 7:52    [CountdownFull] 21:48
MUSIC: Harmony Note Set To: D
MUSIC: Harmony Played: D ~ (fundamentalNoteName + 5)
MUSIC: Harmony Note Set To: E
MUSIC: Harmony Played: E ~ (fundamentalNoteName + 7)
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 20:11    [CountdownThisSection] 7:47    [CountdownFull] 21:43
TimeTrackerScript: [tick] 20:16    [CountdownThisSection] 7:42    [CountdownFull] 21:38
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=0, underflowSamples=0, overflow=1, overflowSamples=512, starvation=0) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
MUSIC: Harmony Note Set To: A
MUSIC: Harmony Played: A ~ (fundamentalNoteName + 12)
[MicVoiceIngest] FAIL_OBSERVATION composite is now TRUE — flags: FAIL_MONITORING_STARVATION_GROWING | micExitReason=copied_samples hzRolling=47.7 maxGapMs=42.18 imitone/callback=0.999 rawLockMisses=66 overflowDrops=0 dbMicSnap=-37.71 imitoneStateStallFrames=0
[MicVoiceIngest] FAIL_OBSERVATION still TRUE after 0.25s — flags: FAIL_MONITORING_STARVATION_GROWING | micExitReason=unread_zero hzRolling=47.7 maxGapMs=42.18 imitone/callback=0.999 rawLockMisses=67 overflowDrops=0 dbMicSnap=-36.34 imitoneStateStallFrames=2
TimeTrackerScript: [tick] 20:21    [CountdownThisSection] 7:37    [CountdownFull] 21:33
[MicVoiceIngest] FAIL_OBSERVATION still TRUE after 0.76s — flags: FAIL_MONITORING_STARVATION_GROWING | micExitReason=copied_samples hzRolling=46.0 maxGapMs=41.11 imitone/callback=0.999 rawLockMisses=67 overflowDrops=0 dbMicSnap=-38.70 imitoneStateStallFrames=0
[MicVoiceIngest] FAIL_OBSERVATION still TRUE after 1.76s — flags: FAIL_MONITORING_STARVATION_GROWING | micExitReason=unread_zero hzRolling=46.7 maxGapMs=41.23 imitone/callback=0.999 rawLockMisses=67 overflowDrops=0 dbMicSnap=-35.57 imitoneStateStallFrames=2
[MicVoiceIngest] FAIL_OBSERVATION cleared after 2.01s — flags seen during window: FAIL_MONITORING_STARVATION_GROWING 
TimeTrackerScript: [tick] 20:26    [CountdownThisSection] 7:33    [CountdownFull] 21:28
AVS FXWave started with key: 31
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=1, overflowSamples=512, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
MUSIC: Harmony Note Set To: D
MUSIC: Harmony Played: D ~ (fundamentalNoteName + 5)
TimeTrackerScript: [tick] 20:31    [CountdownThisSection] 7:28    [CountdownFull] 21:23
TimeTrackerScript: [tick] 20:36    [CountdownThisSection] 7:23    [CountdownFull] 21:18
AVS FXWave started with key: 32
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=1, overflowSamples=512, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 20:41    [CountdownThisSection] 7:18    [CountdownFull] 21:13
MUSIC: Harmony Note Set To: E
MUSIC: Harmony Played: E ~ (fundamentalNoteName + 7)
Director Change Detection REST: [ANCHOR SET] (3.235145)|(6.48352)
Director Queue: Removed all monostereo items from director queue.
Director Queue: Added 8 monostereo to director queue.
MUSIC FUNDAMENTAL-CONTENT-LOCK: Tried to unlock fundamental content, but it was already unlocked
MUSIC: Soundscape Set To: Shadow (SoundWorld)
MUSIC TEST: currentInteractionType after SetSoundscape: SoundWorld
Director: Transition Sound Played
WorldShuffler: Starting shuffle timer.
Director Queue: Action SoundscapeShuffle executed from process-all
Preferred color set to: White
Transition to White2 over 2 s with new currentColorType of White
WorldShuffler: Starting shuffle timer.
Director Queue: Action ColorWorldShuffle executed from process-all
AVS: Bilateral switch command changed to False, but no change in state. Ignoring.
Director Queue: Action monostereo executed from process-all
TimeTrackerScript: [tick] 20:46    [CountdownThisSection] 7:13    [CountdownFull] 21:08
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=1, overflowSamples=512, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
Director: Transition Sound cooldown expired - can play again.
TimeTrackerScript: [tick] 20:51    [CountdownThisSection] 7:08    [CountdownFull] 21:03
MUSIC: Harmony Note Set To: D
MUSIC: Harmony Played: D ~ (fundamentalNoteName + 5)
Director Change Detection REST: unanchored with range threshold(0.065)
Director Queue: ActivateQueue called but queue is empty
Director Change Detection: Breath Length Change
MUSIC: Harmony Note Set To: E
MUSIC: Harmony Played: E ~ (fundamentalNoteName + 7)
TimeTrackerScript: [tick] 20:56    [CountdownThisSection] 7:03    [CountdownFull] 20:58
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=1, overflowSamples=512, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 21:01    [CountdownThisSection] 6:58    [CountdownFull] 20:53
MUSIC: Harmony Note Set To: D
MUSIC: Harmony Played: D ~ (fundamentalNoteName + 5)
TimeTrackerScript: [tick] 21:06    [CountdownThisSection] 6:53    [CountdownFull] 20:48
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=1, overflowSamples=512, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 21:11    [CountdownThisSection] 6:48    [CountdownFull] 20:43
MUSIC: Harmony Note Set To: E
MUSIC: Harmony Played: E ~ (fundamentalNoteName + 7)
MUSIC: Harmony Note Set To: D
MUSIC: Harmony Played: D ~ (fundamentalNoteName + 5)
TimeTrackerScript: [tick] 21:16    [CountdownThisSection] 6:43    [CountdownFull] 20:38
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=1, overflowSamples=512, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 21:21    [CountdownThisSection] 6:38    [CountdownFull] 20:33
TimeTrackerScript: [tick] 21:26    [CountdownThisSection] 6:33    [CountdownFull] 20:28
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 21:31    [CountdownThisSection] 6:28    [CountdownFull] 20:23
TimeTrackerScript: [tick] 21:36    [CountdownThisSection] 6:23    [CountdownFull] 20:18
MUSIC: Harmony Note Set To: E
MUSIC: Harmony Played: E ~ (fundamentalNoteName + 7)
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 21:41    [CountdownThisSection] 6:18    [CountdownFull] 20:13
MUSIC: Harmony Note Set To: D
MUSIC: Harmony Played: D ~ (fundamentalNoteName + 5)
TimeTrackerScript: [tick] 21:46    [CountdownThisSection] 6:13    [CountdownFull] 20:08
MUSIC: Harmony Note Set To: E
MUSIC: Harmony Played: E ~ (fundamentalNoteName + 7)
WorldShuffler: Time to shuffle worlds.
WorldShuffler: Queuing World Shuffle
Director Queue: Added 9 SoundscapeShuffle to director queue.
Director Queue: Added 10 ColorWorldShuffle to director queue.
WorldShuffler: Waiting for next shuffle to begin timer.
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 21:51    [CountdownThisSection] 6:08    [CountdownFull] 20:03
MUSIC: Harmony Note Set To: A
MUSIC: Harmony Played: A ~ (fundamentalNoteName + 12)
TimeTrackerScript: [tick] 21:56    [CountdownThisSection] 6:03    [CountdownFull] 19:58
MUSIC FUNDAMENTAL-CONTENT-LOCK: Tried to unlock fundamental content, but it was already unlocked
MUSIC: Soundscape Set To: Shruti (SoundWorld)
MUSIC TEST: currentInteractionType after SetSoundscape: SoundWorld
Director: Transition Sound Played
WorldShuffler: Starting shuffle timer.
Director Queue: Action SoundscapeShuffle executed from process-all
Preferred color set to: Red
Transition to Red3 over 2 s with new currentColorType of Red
WorldShuffler: Starting shuffle timer.
Director Queue: Action ColorWorldShuffle executed from process-all
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 22:01    [CountdownThisSection] 5:58    [CountdownFull] 19:53
Director: Transition Sound cooldown expired - can play again.
TimeTrackerScript: [tick] 22:06    [CountdownThisSection] 5:53    [CountdownFull] 19:48
MUSIC: Harmony Note Set To: D
MUSIC: Harmony Played: D ~ (fundamentalNoteName + 5)
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 22:11    [CountdownThisSection] 5:48    [CountdownFull] 19:43
TimeTrackerScript: [tick] 22:16    [CountdownThisSection] 5:43    [CountdownFull] 19:38
MUSIC: Harmony Note Set To: E
MUSIC: Harmony Played: E ~ (fundamentalNoteName + 7)
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 22:21    [CountdownThisSection] 5:38    [CountdownFull] 19:33
MUSIC: Harmony Note Set To: A
MUSIC: Harmony Played: A ~ (fundamentalNoteName + 12)
Director Change Detection REST: [ANCHOR SET] (8.85628)|(9.809749)
TimeTrackerScript: [tick] 22:26    [CountdownThisSection] 5:33    [CountdownFull] 19:27
MUSIC: Harmony Note Set To: D
MUSIC: Harmony Played: D ~ (fundamentalNoteName + 5)
Director Change Detection REST: unanchored with range threshold(0.065)
Director Change Detection: Breath Length Change
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 22:31    [CountdownThisSection] 5:28    [CountdownFull] 19:22
MUSIC: Harmony Note Set To: E
MUSIC: Harmony Played: E ~ (fundamentalNoteName + 7)
TimeTrackerScript: [tick] 22:36    [CountdownThisSection] 5:23    [CountdownFull] 19:17
Director Change Detection TONE: [ANCHOR SET] (1.756547)|(3.086699)
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 22:41    [CountdownThisSection] 5:19    [CountdownFull] 19:12
MUSIC: Harmony Note Set To: A
MUSIC: Harmony Played: A ~ (fundamentalNoteName + 12)
Director Change Detection TONE: unanchored with range threshold(0.1)
Director Queue: ActivateQueue called but queue is empty
Director Change Detection: Tone Length Change
TimeTrackerScript: [tick] 22:46    [CountdownThisSection] 5:14    [CountdownFull] 19:07
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 22:51    [CountdownThisSection] 5:09    [CountdownFull] 19:02
TimeTrackerScript: [tick] 22:56    [CountdownThisSection] 5:04    [CountdownFull] 18:57
WorldShuffler: Time to shuffle worlds.
WorldShuffler: Queuing World Shuffle
Director Queue: Added 11 SoundscapeShuffle to director queue.
Director Queue: Added 12 ColorWorldShuffle to director queue.
WorldShuffler: Waiting for next shuffle to begin timer.
Director Queue: Removed all monostereo items from director queue.
Director Queue: Added 13 monostereo to director queue.
Binaural Beats: New Rate is 5.75
Strobe Rate set to: 8.5 Hz over 180000 ms
Binaural Beats: Lerping Binaural Beat Rate from 5.75 to 4.25
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 23:01    [CountdownThisSection] 4:59    [CountdownFull] 18:52
TimeTrackerScript: [tick] 23:06    [CountdownThisSection] 4:54    [CountdownFull] 18:47
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 23:11    [CountdownThisSection] 4:49    [CountdownFull] 18:42
MUSIC: Harmony Note Set To: D
MUSIC: Harmony Played: D ~ (fundamentalNoteName + 5)
TimeTrackerScript: [tick] 23:16    [CountdownThisSection] 4:44    [CountdownFull] 18:37
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 23:21    [CountdownThisSection] 4:39    [CountdownFull] 18:32
AVS FXWave started with key: 33
TimeTrackerScript: [tick] 23:26    [CountdownThisSection] 4:34    [CountdownFull] 18:27
MUSIC: Harmony Note Set To: E
MUSIC: Harmony Played: E ~ (fundamentalNoteName + 7)
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 23:31    [CountdownThisSection] 4:29    [CountdownFull] 18:22
TimeTrackerScript: [tick] 23:36    [CountdownThisSection] 4:24    [CountdownFull] 18:17
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
WorldShuffler: Resetting all color worlds to be available for shuffling.
PlaygroundStageHandler(Standard): Start2 at 300s — reset color worlds.
WorldShuffler: Resetting all soundscapes to be available for shuffling.
WorldShuffler: Excluding Soundscape -Shadow- from shuffle.
WorldShuffler: Excluding Soundscape -Shruti- from shuffle.
PlaygroundStageHandler(Standard): End1 at <=300s — exclude Shadow/Shruti.
TimeTrackerScript: [tick] 23:41    [CountdownThisSection] 4:19    [CountdownFull] 18:12
Director Queue: Removed all monostereo items from director queue.
Director Queue: Added 14 monostereo to director queue.
TimeTrackerScript: [tick] 23:46    [CountdownThisSection] 4:14    [CountdownFull] 18:07
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 23:51    [CountdownThisSection] 4:09    [CountdownFull] 18:02
TimeTrackerScript: [tick] 23:56    [CountdownThisSection] 4:04    [CountdownFull] 17:57
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 24:01    [CountdownThisSection] 3:59    [CountdownFull] 17:52
TimeTrackerScript: [tick] 24:06    [CountdownThisSection] 3:54    [CountdownFull] 17:47
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 24:11    [CountdownThisSection] 3:49    [CountdownFull] 17:42
TimeTrackerScript: [tick] 24:16    [CountdownThisSection] 3:44    [CountdownFull] 17:37
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 24:21    [CountdownThisSection] 3:39    [CountdownFull] 17:32
TimeTrackerScript: [tick] 24:26    [CountdownThisSection] 3:34    [CountdownFull] 17:27
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 24:31    [CountdownThisSection] 3:29    [CountdownFull] 17:22
TimeTrackerScript: [tick] 24:36    [CountdownThisSection] 3:24    [CountdownFull] 17:17
MUSIC: Harmony Note Set To: A
MUSIC: Harmony Played: A ~ (fundamentalNoteName + 12)
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 24:41    [CountdownThisSection] 3:19    [CountdownFull] 17:12
Director Queue: Action 14 monostereo expired, will activate on next tone...
Director Queue: Action 14 monostereo will activate when next tone begins
TimeTrackerScript: [tick] 24:46    [CountdownThisSection] 3:14    [CountdownFull] 17:07
TimeTrackerScript: [tick] 24:51    [CountdownThisSection] 3:09    [CountdownFull] 17:02
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
MUSIC: Harmony Note Set To: D
MUSIC: Harmony Played: D ~ (fundamentalNoteName + 5)
Director Queue: Action 14 monostereo activating with tone
AVS: Bilateral switch command changed to False, but no change in state. Ignoring.
Director Change Detection REST: [ANCHOR SET] (21.76781)|(28.72796)
TimeTrackerScript: [tick] 24:56    [CountdownThisSection] 3:04    [CountdownFull] 16:57
Director Queue: Action 11 SoundscapeShuffle expired, will activate entire queue on next tone...
Director Queue: Entire queue will activate when next tone begins
Director Queue: Action 12 ColorWorldShuffle expired, will activate entire queue on next tone...
Director Queue: Removed all Soundscape items from director queue.
Director Queue: Added 15 Soundscape to director queue.
Director Queue: Removed all TransitionSound items from director queue.
Director Queue: Added 16 TransitionSound to director queue.
PlaygroundStageHandler(Standard): End2 at <=180s — queue Shruti + transition + dynamic drop end.
Cleanup: Key -1 not found in director.queue, skipping.
Cleanup: Key -1 not found in director.queue, skipping.
Cleanup: Key 0 not found in director.queue, skipping.
Cleanup: Key 1 not found in director.queue, skipping.
Cleanup: Key 7 not found in director.queue, skipping.
Cleanup: Key 8 not found in director.queue, skipping.
Cleanup: Key 13 not found in director.queue, skipping.
Cleanup: Key 14 not found in director.queue, skipping.
Director Queue Contents: <16 TransitionSound, 179.9939s> <15 Soundscape, 179.9939s> 
Director Queue: Removed all gamma items from director queue.
Director Queue: Removed all monostereo items from director queue.
Director Queue: Removed all monostereo items from director queue.
Director Queue: Added 17 monostereo to director queue.
TimeTrackerScript: [tick] 25:01    [CountdownThisSection] 2:59    [CountdownFull] 16:52
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
MUSIC: Harmony Note Set To: E
MUSIC: Harmony Played: E ~ (fundamentalNoteName + 7)
Director Queue: Activating entire queue with tone
Director: Transition Sound Played
Director Queue: Action TransitionSound executed from process-all
MUSIC FUNDAMENTAL-CONTENT-LOCK: Tried to unlock fundamental content, but it was already unlocked
MUSIC: Soundscape Set To: Shruti (SoundWorld)
MUSIC TEST: currentInteractionType after SetSoundscape: SoundWorld
Director Queue: Action Soundscape executed from process-all
AVS: Switching to Stereo
Director Queue: Action monostereo executed from process-all
Director Change Detection REST: unanchored with range threshold(0.065)
Director Queue: ActivateQueue called but queue is empty
Director Change Detection: Breath Length Change
TimeTrackerScript: [tick] 25:06    [CountdownThisSection] 2:54    [CountdownFull] 16:48
Director: Transition Sound cooldown expired - can play again.
TimeTrackerScript: [tick] 25:11    [CountdownThisSection] 2:49    [CountdownFull] 16:43
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
MUSIC: Harmony Note Set To: A
MUSIC: Harmony Played: A ~ (fundamentalNoteName + 12)
TimeTrackerScript: [tick] 25:16    [CountdownThisSection] 2:45    [CountdownFull] 16:38
TimeTrackerScript: [tick] 25:21    [CountdownThisSection] 2:40    [CountdownFull] 16:33
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
AVS FXWave started with key: 34
TimeTrackerScript: [tick] 25:26    [CountdownThisSection] 2:35    [CountdownFull] 16:28
MUSIC: Harmony Note Set To: D
MUSIC: Harmony Played: D ~ (fundamentalNoteName + 5)
TimeTrackerScript: [tick] 25:31    [CountdownThisSection] 2:30    [CountdownFull] 16:23
Director Queue: ActivateQueue called but queue is empty
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 25:36    [CountdownThisSection] 2:25    [CountdownFull] 16:18
MUSIC: Harmony Note Set To: E
MUSIC: Harmony Played: E ~ (fundamentalNoteName + 7)
TimeTrackerScript: [tick] 25:41    [CountdownThisSection] 2:20    [CountdownFull] 16:13
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 25:46    [CountdownThisSection] 2:15    [CountdownFull] 16:08
TimeTrackerScript: [tick] 25:51    [CountdownThisSection] 2:10    [CountdownFull] 16:03
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 25:56    [CountdownThisSection] 2:05    [CountdownFull] 15:58
MUSIC: Harmony Note Set To: A
MUSIC: Harmony Played: A ~ (fundamentalNoteName + 12)
TimeTrackerScript: [tick] 26:01    [CountdownThisSection] 2:00    [CountdownFull] 15:53
Binaural Beats: New Rate is 4.25
Strobe Rate set to: 11.5 Hz over 180000 ms
Binaural Beats: Lerping Binaural Beat Rate from 4.25 to 5.75
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
AVS FXWave started with key: 35
TimeTrackerScript: [tick] 26:06    [CountdownThisSection] 1:55    [CountdownFull] 15:48
TimeTrackerScript: [tick] 26:11    [CountdownThisSection] 1:50    [CountdownFull] 15:43
Director Queue: Removed all monostereo items from director queue.
Director Queue: Added 18 monostereo to director queue.
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
MUSIC: Harmony Note Set To: D
MUSIC: Harmony Played: D ~ (fundamentalNoteName + 5)
TimeTrackerScript: [tick] 26:16    [CountdownThisSection] 1:45    [CountdownFull] 15:38
MUSIC: Harmony Note Set To: E
MUSIC: Harmony Played: E ~ (fundamentalNoteName + 7)
TimeTrackerScript: [tick] 26:21    [CountdownThisSection] 1:40    [CountdownFull] 15:33
Director Queue: Action 18 monostereo expired, will activate on next tone...
Director Queue: Action 18 monostereo will activate when next tone begins
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 26:26    [CountdownThisSection] 1:35    [CountdownFull] 15:28
MUSIC: Harmony Note Set To: A
MUSIC: Harmony Played: A ~ (fundamentalNoteName + 12)
Director Queue: Action 18 monostereo activating with tone
AVS: Switching to Mono
TimeTrackerScript: [tick] 26:31    [CountdownThisSection] 1:30    [CountdownFull] 15:23
Saw Strobe Coroutine stopped
Binaural Beats: Lerping Binaural Beat Rate from 4.499748 to 5
Strobe Rate set to: 40 Hz over 90000 ms
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 26:36    [CountdownThisSection] 1:25    [CountdownFull] 15:18
MUSIC: Harmony Note Set To: D
MUSIC: Harmony Played: D ~ (fundamentalNoteName + 5)
Director Change Detection REST: [ANCHOR SET] (6.964126)|(8.320951)
Binaural Beats: New Rate is 5
TimeTrackerScript: [tick] 26:41    [CountdownThisSection] 1:20    [CountdownFull] 15:13
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
AVS FXWave started with key: 36
TimeTrackerScript: [tick] 26:46    [CountdownThisSection] 1:15    [CountdownFull] 15:08
TimeTrackerScript: [tick] 26:51    [CountdownThisSection] 1:10    [CountdownFull] 15:03
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
MUSIC: Harmony Note Set To: E
MUSIC: Harmony Played: E ~ (fundamentalNoteName + 7)
TimeTrackerScript: [tick] 26:56    [CountdownThisSection] 1:05    [CountdownFull] 14:58
TimeTrackerScript: [tick] 27:01    [CountdownThisSection] 1:00    [CountdownFull] 14:53
PlaygroundStageHandler(Standard): End3 at <=60s — starting LastMinute behavior.
Director Queue: Removed all SoundscapeShuffle items from director queue.
Director Queue: Removed all ColorWorldShuffle items from director queue.
WorldShuffler: Stopping shuffle.
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
AVS FXWave started with key: 37
Switching to Meditative Mode
TimeTrackerScript: [tick] 27:06    [CountdownThisSection] 0:55    [CountdownFull] 14:48
MUSIC: Harmony Note Set To: A
MUSIC: Harmony Played: A ~ (fundamentalNoteName + 12)
MUSIC: Harmony Note Set To: E
MUSIC: Harmony Played: E ~ (fundamentalNoteName + 7)
Director Change Detection REST: unanchored with range threshold(0.065)
Director Queue: ActivateQueue called but queue is empty
MUSIC: Setting Music Mode to FrozenFreeplay...
Binaural Beats: Lerping Volume from 70 to 70over 5 seconds
MUSIC: Music Mode Set to FrozenFreeplay
MUSIC 6: Fundamental Note Changing to C
Director Queue: Removed all fundamentalChange items from director queue.
Binaural Beats: Fading Out Binaural Beats with Stop event for frequency change to 261.6255 Hz
MUSIC 8: Key(C: ChangeFundamentalTimer reset
MUSIC 8: Key(Cs: ChangeFundamentalTimer reset
MUSIC 8: Key(D: ChangeFundamentalTimer reset
MUSIC 8: Key(Ds: ChangeFundamentalTimer reset
MUSIC 8: Key(E: ChangeFundamentalTimer reset
MUSIC 8: Key(F: ChangeFundamentalTimer reset
MUSIC 8: Key(Fs: ChangeFundamentalTimer reset
MUSIC 8: Key(G: ChangeFundamentalTimer reset
MUSIC 8: Key(Gs: ChangeFundamentalTimer reset
MUSIC 8: Key(A: ChangeFundamentalTimer reset
MUSIC 8: Key(As: ChangeFundamentalTimer reset
MUSIC 8: Key(B: ChangeFundamentalTimer reset
MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Locked to C
MUSIC: Interactive Music Mode Recovered to InteractiveMusicSystem because Interaction Type is SoundWorld
Director Queue: Director is disabled, not activating queue.
Director Change Detection: Breath Length Change
Director Queue: Director Disabled
Switching to Playful Mode
TimeTrackerScript: [tick] 27:11    [CountdownThisSection] 0:50    [CountdownFull] 14:43
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
Binaural Beats: Changing Center Frequency to 261.6255 Hz, and fading in again with Play event
Binaural Beats: New Volume is 70
TimeTrackerScript: [tick] 27:16    [CountdownThisSection] 0:45    [CountdownFull] 14:39
TimeTrackerScript: [tick] 27:21    [CountdownThisSection] 0:40    [CountdownFull] 14:34
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 27:26    [CountdownThisSection] 0:35    [CountdownFull] 14:29
TimeTrackerScript: [tick] 27:31    [CountdownThisSection] 0:30    [CountdownFull] 14:24
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 27:36    [CountdownThisSection] 0:25    [CountdownFull] 14:19
TimeTrackerScript: [tick] 27:41    [CountdownThisSection] 0:20    [CountdownFull] 14:14
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 27:46    [CountdownThisSection] 0:15    [CountdownFull] 14:09
MUSIC: Setting Music Mode to Environment...
Binaural Beats: Lerping Volume from 70 to 0over 5 seconds
MUSIC: Music Mode Set to Environment (MusicEnvironmentMode State + ambient)
MUSIC_LINEAR: Play — State=Environment, PostEvent Play_AMBIENT_ENVIRONMENT_LOOP
MUSIC: EnterMusicEnvironmentAudio — delegated to MusicSystemLinear.Play()
Preferred color set to: Dark
Transition to Dark over 18 s with new currentColorType of Dark
Going Dark - 18
Tutorial: StopTutorial called, but tutorial is not active.
TimeTrackerScript: [tick] 27:51    [CountdownThisSection] 0:10    [CountdownFull] 14:04
Binaural Beats: New Volume is 0
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=3, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 27:56    [CountdownThisSection] 0:06    [CountdownFull] 13:59
TimeTrackerScript: [tick] 28:01    [CountdownThisSection] 0:01    [CountdownFull] 13:54
PlaygroundStageHandler: Marking stage complete.
[StartCountdown] Enter variant=Countdown_ClosingDuration.
[StartCountdown] TimeTracker state before this stage: [CountdownThisSection]=0:00 (0s) [CountdownFull]=13:53 (833s) IsCountdownRunning=True ConfiguredFullAtLastConfigure=2400s
[StartCountdown] Countdown_ClosingDuration: applying post-unguided pair both = 14:00 (840s) (raw closing from CSV/tracker 840.0 s). Preserved full baseline for HUD (if any) = 2400.0 s. Before: ThisSection=0:00 (0s) Full=13:53 (833s).
TimeTrackerScript: BeginCountdownPair re-entry — [CountdownThisSection] was 0, [CountdownFull] was 833.3077; new starts 840 / 840.
[StartCountdown] Countdown_ClosingDuration: after ConfigureCountdownPair + BeginCountdownPair (+ baseline restore): [CountdownThisSection]=14:00 (840s) [CountdownFull]=14:00 (840s) IsCountdownRunning=True ConfiguredFullAtLastConfigure=2400s
[StartCountdown] Stage complete (IsComplete=true).
Sequence: Entered stage 6 (StartCountdown)
WWise_VO: Stop Opening Sequence
SavasanaStageHandler: Enter - running Savasana for variant 'Savasana_Standard'.
MUSIC 6: Fundamental Note Changing to C
Director Queue: Removed all fundamentalChange items from director queue.
MUSIC 8: Key(C: ChangeFundamentalTimer reset
MUSIC 8: Key(Cs: ChangeFundamentalTimer reset
MUSIC 8: Key(D: ChangeFundamentalTimer reset
MUSIC 8: Key(Ds: ChangeFundamentalTimer reset
MUSIC 8: Key(E: ChangeFundamentalTimer reset
MUSIC 8: Key(F: ChangeFundamentalTimer reset
MUSIC 8: Key(Fs: ChangeFundamentalTimer reset
MUSIC 8: Key(G: ChangeFundamentalTimer reset
MUSIC 8: Key(Gs: ChangeFundamentalTimer reset
MUSIC 8: Key(A: ChangeFundamentalTimer reset
MUSIC 8: Key(As: ChangeFundamentalTimer reset
MUSIC 8: Key(B: ChangeFundamentalTimer reset
MUSIC FUNDAMENTAL-CONTENT-LOCK: Fundamental Content Locked to C
Director Queue: Director is disabled, not activating queue.
MUSIC: Setting Music Mode to MusicLoopSilent...
MUSIC_LINEAR: Stop — State=Music, scheduling Stop_AMBIENT_ENVIRONMENT_LOOP in 10.0s
MUSIC: ExitMusicEnvironmentAudio — delegated to MusicSystemLinear.Stop() (delayed exit handled inside the delegate).
MUSIC: Setting Music Binaural Beats volume to 50.0f for MusicLoopSilent mode
Binaural Beats: Lerping Volume from 0 to 50over 5 seconds
MUSIC: Music Mode Set to MusicLoopSilent, which is a temporary mode for a musicloop version of silent mode, before we merge the two silent modes.
SavasanaStageHandler: Playing Thematic savasana VO.
Sequence: Entered stage 7 (Savasana)
WWise_VO: Callback triggered: AK_MusicSyncUserCue
WWise_VO: Cue_ThematicSavasana_Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
AVS: Stopping Reference Signal
TimeTrackerScript: [tick] 28:06    [CountdownThisSection] 13:56    [CountdownFull] 13:56
Binaural Beats: New Volume is 50
WWise_VO: Callback triggered: AK_MusicSyncUserCue
WWise_VO: Cue_ThematicSavasana_Start
WWise_VO: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue Mic OFF
TimeTrackerScript: [tick] 28:11    [CountdownThisSection] 13:51    [CountdownFull] 13:51
MUSIC_LINEAR: delayed — PostEvent Stop_AMBIENT_ENVIRONMENT_LOOP
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 28:16    [CountdownThisSection] 13:46    [CountdownFull] 13:46
TimeTrackerScript: [tick] 28:21    [CountdownThisSection] 13:41    [CountdownFull] 13:41
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 28:26    [CountdownThisSection] 13:36    [CountdownFull] 13:36
TimeTrackerScript: [tick] 28:31    [CountdownThisSection] 13:31    [CountdownFull] 13:31
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 28:36    [CountdownThisSection] 13:26    [CountdownFull] 13:26
TimeTrackerScript: [tick] 28:41    [CountdownThisSection] 13:21    [CountdownFull] 13:21
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 28:46    [CountdownThisSection] 13:16    [CountdownFull] 13:16
TimeTrackerScript: [tick] 28:51    [CountdownThisSection] 13:11    [CountdownFull] 13:11
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 28:56    [CountdownThisSection] 13:06    [CountdownFull] 13:06
TimeTrackerScript: [tick] 29:01    [CountdownThisSection] 13:01    [CountdownFull] 13:01
Binaural Beats: New Rate is 5.75
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 29:06    [CountdownThisSection] 12:56    [CountdownFull] 12:56
TimeTrackerScript: [tick] 29:11    [CountdownThisSection] 12:51    [CountdownFull] 12:51
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 29:16    [CountdownThisSection] 12:46    [CountdownFull] 12:46
TimeTrackerScript: [tick] 29:21    [CountdownThisSection] 12:41    [CountdownFull] 12:41
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 29:26    [CountdownThisSection] 12:36    [CountdownFull] 12:36
TimeTrackerScript: [tick] 29:31    [CountdownThisSection] 12:31    [CountdownFull] 12:31
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 29:36    [CountdownThisSection] 12:26    [CountdownFull] 12:26
TimeTrackerScript: [tick] 29:41    [CountdownThisSection] 12:21    [CountdownFull] 12:21
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 29:46    [CountdownThisSection] 12:16    [CountdownFull] 12:16
TimeTrackerScript: [tick] 29:51    [CountdownThisSection] 12:11    [CountdownFull] 12:11
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 29:56    [CountdownThisSection] 12:06    [CountdownFull] 12:06
TimeTrackerScript: [tick] 30:01    [CountdownThisSection] 12:02    [CountdownFull] 12:02
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 30:06    [CountdownThisSection] 11:57    [CountdownFull] 11:57
TimeTrackerScript: [tick] 30:11    [CountdownThisSection] 11:52    [CountdownFull] 11:52
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 30:16    [CountdownThisSection] 11:47    [CountdownFull] 11:47
TimeTrackerScript: [tick] 30:21    [CountdownThisSection] 11:42    [CountdownFull] 11:42
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 30:26    [CountdownThisSection] 11:37    [CountdownFull] 11:37
TimeTrackerScript: [tick] 30:31    [CountdownThisSection] 11:32    [CountdownFull] 11:32
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 30:36    [CountdownThisSection] 11:27    [CountdownFull] 11:27
TimeTrackerScript: [tick] 30:41    [CountdownThisSection] 11:22    [CountdownFull] 11:22
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 30:46    [CountdownThisSection] 11:17    [CountdownFull] 11:17
TimeTrackerScript: [tick] 30:51    [CountdownThisSection] 11:12    [CountdownFull] 11:12
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 30:56    [CountdownThisSection] 11:07    [CountdownFull] 11:07
TimeTrackerScript: [tick] 31:01    [CountdownThisSection] 11:02    [CountdownFull] 11:02
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
[MicVoiceIngest] FAIL_OBSERVATION composite is now TRUE — flags: FAIL_AUDIO_CALLBACK_GAP_HIGH | micExitReason=copied_samples hzRolling=46.7 maxGapMs=42.73 imitone/callback=0.999 rawLockMisses=117 overflowDrops=0 dbMicSnap=-63.36 imitoneStateStallFrames=9768
[MicVoiceIngest] FAIL_OBSERVATION still TRUE after 0.25s — flags: FAIL_AUDIO_CALLBACK_GAP_HIGH | micExitReason=unread_zero hzRolling=46.7 maxGapMs=42.73 imitone/callback=0.999 rawLockMisses=117 overflowDrops=0 dbMicSnap=-73.08 imitoneStateStallFrames=9804
[MicVoiceIngest] FAIL_OBSERVATION still TRUE after 0.76s — flags: FAIL_AUDIO_CALLBACK_GAP_HIGH | micExitReason=unread_zero hzRolling=46.7 maxGapMs=42.73 imitone/callback=0.999 rawLockMisses=117 overflowDrops=0 dbMicSnap=-66.01 imitoneStateStallFrames=9887
[MicVoiceIngest] FAIL_OBSERVATION cleared after 1.01s — flags seen during window: FAIL_AUDIO_CALLBACK_GAP_HIGH 
TimeTrackerScript: [tick] 31:06    [CountdownThisSection] 10:57    [CountdownFull] 10:57
TimeTrackerScript: [tick] 31:11    [CountdownThisSection] 10:52    [CountdownFull] 10:52
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 31:16    [CountdownThisSection] 10:47    [CountdownFull] 10:47
TimeTrackerScript: [tick] 31:21    [CountdownThisSection] 10:42    [CountdownFull] 10:42
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 31:26    [CountdownThisSection] 10:37    [CountdownFull] 10:37
TimeTrackerScript: [tick] 31:31    [CountdownThisSection] 10:32    [CountdownFull] 10:32
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 31:36    [CountdownThisSection] 10:27    [CountdownFull] 10:27
TimeTrackerScript: [tick] 31:41    [CountdownThisSection] 10:22    [CountdownFull] 10:22
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 31:46    [CountdownThisSection] 10:17    [CountdownFull] 10:17
TimeTrackerScript: [tick] 31:51    [CountdownThisSection] 10:12    [CountdownFull] 10:12
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 31:56    [CountdownThisSection] 10:07    [CountdownFull] 10:07
WWise_VO: Callback triggered: AK_MusicSyncUserCue
Wwise_VO: Cue_ThematicSavasana_End
ThematicSavasana_End fired but it's not being watched for, so nothing is happening.
TimeTrackerScript: [tick] 32:01    [CountdownThisSection] 10:02    [CountdownFull] 10:02
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 32:06    [CountdownThisSection] 9:57    [CountdownFull] 9:57
TimeTrackerScript: [tick] 32:11    [CountdownThisSection] 9:53    [CountdownFull] 9:53
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 32:16    [CountdownThisSection] 9:48    [CountdownFull] 9:48
TimeTrackerScript: [tick] 32:21    [CountdownThisSection] 9:43    [CountdownFull] 9:43
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 32:26    [CountdownThisSection] 9:38    [CountdownFull] 9:38
TimeTrackerScript: [tick] 32:31    [CountdownThisSection] 9:33    [CountdownFull] 9:33
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 32:36    [CountdownThisSection] 9:28    [CountdownFull] 9:28
TimeTrackerScript: [tick] 32:41    [CountdownThisSection] 9:23    [CountdownFull] 9:23
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 32:46    [CountdownThisSection] 9:18    [CountdownFull] 9:18
TimeTrackerScript: [tick] 32:51    [CountdownThisSection] 9:13    [CountdownFull] 9:13
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 32:57    [CountdownThisSection] 9:08    [CountdownFull] 9:08
TimeTrackerScript: [tick] 33:02    [CountdownThisSection] 9:03    [CountdownFull] 9:03
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 33:07    [CountdownThisSection] 8:58    [CountdownFull] 8:58
TimeTrackerScript: [tick] 33:12    [CountdownThisSection] 8:53    [CountdownFull] 8:53
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 33:17    [CountdownThisSection] 8:48    [CountdownFull] 8:48
TimeTrackerScript: [tick] 33:22    [CountdownThisSection] 8:43    [CountdownFull] 8:43
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 33:27    [CountdownThisSection] 8:38    [CountdownFull] 8:38
TimeTrackerScript: [tick] 33:32    [CountdownThisSection] 8:33    [CountdownFull] 8:33
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 33:37    [CountdownThisSection] 8:28    [CountdownFull] 8:28
TimeTrackerScript: [tick] 33:42    [CountdownThisSection] 8:23    [CountdownFull] 8:23
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 33:47    [CountdownThisSection] 8:18    [CountdownFull] 8:18
TimeTrackerScript: [tick] 33:52    [CountdownThisSection] 8:13    [CountdownFull] 8:13
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 33:57    [CountdownThisSection] 8:08    [CountdownFull] 8:08
WWise_VO: Callback triggered: AK_MusicSyncUserCue
Wwise_VO: Cue_VO_Wakeup_Start
TimeTrackerScript: [tick] 34:02    [CountdownThisSection] 8:03    [CountdownFull] 8:03
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 34:07    [CountdownThisSection] 7:58    [CountdownFull] 7:58
TimeTrackerScript: [tick] 34:12    [CountdownThisSection] 7:53    [CountdownFull] 7:53
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 34:17    [CountdownThisSection] 7:48    [CountdownFull] 7:48
TimeTrackerScript: [tick] 34:22    [CountdownThisSection] 7:44    [CountdownFull] 7:44
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 34:27    [CountdownThisSection] 7:39    [CountdownFull] 7:39
TimeTrackerScript: [tick] 34:32    [CountdownThisSection] 7:34    [CountdownFull] 7:34
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 34:37    [CountdownThisSection] 7:29    [CountdownFull] 7:29
TimeTrackerScript: [tick] 34:42    [CountdownThisSection] 7:24    [CountdownFull] 7:24
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 34:47    [CountdownThisSection] 7:19    [CountdownFull] 7:19
TimeTrackerScript: [tick] 34:52    [CountdownThisSection] 7:14    [CountdownFull] 7:14
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 34:57    [CountdownThisSection] 7:09    [CountdownFull] 7:09
WWise_VO: Callback triggered: AK_MusicSyncUserCue
WWise_VO: Unexpected Cue: AK_MusicSyncUserCue | Cue_VO_EndingSoon_Start
TimeTrackerScript: [tick] 35:02    [CountdownThisSection] 7:04    [CountdownFull] 7:04
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 35:07    [CountdownThisSection] 6:59    [CountdownFull] 6:59
WWise_VO: Callback triggered: AK_MusicSyncUserCue
Wwise_VO: Cue_VoiceElicitation2_Start
WWise_VO: Callback triggered: AK_MusicSyncUserCue
Wwise_VO: Cue_VoiceElicitation2_Start
TimeTrackerScript: [tick] 35:12    [CountdownThisSection] 6:54    [CountdownFull] 6:54
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 35:17    [CountdownThisSection] 6:49    [CountdownFull] 6:49
WWise_VO: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue Mic On
TimeTrackerScript: [tick] 35:22    [CountdownThisSection] 6:44    [CountdownFull] 6:44
WWise_VO: Callback triggered: AK_MusicSyncUserCue
WWise_VO: Unexpected Cue: AK_MusicSyncUserCue | Cue_BreathIn_Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 35:27    [CountdownThisSection] 6:39    [CountdownFull] 6:39
WWise_VO: Callback triggered: AK_MusicSyncUserCue
WWise_VO: Unexpected Cue: AK_MusicSyncUserCue | Cue_Sigh_Start
TimeTrackerScript: [tick] 35:32    [CountdownThisSection] 6:34    [CountdownFull] 6:34
WWise_VO: Callback triggered: AK_MusicSyncUserCue
WWise_VO_CUE: Cue Mic OFF
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 35:37    [CountdownThisSection] 6:29    [CountdownFull] 6:29
TimeTrackerScript: [tick] 35:42    [CountdownThisSection] 6:24    [CountdownFull] 6:24
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 35:47    [CountdownThisSection] 6:19    [CountdownFull] 6:19
TimeTrackerScript: [tick] 35:52    [CountdownThisSection] 6:14    [CountdownFull] 6:14
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 35:57    [CountdownThisSection] 6:09    [CountdownFull] 6:09
TimeTrackerScript: [tick] 36:02    [CountdownThisSection] 6:04    [CountdownFull] 6:04
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 36:07    [CountdownThisSection] 5:59    [CountdownFull] 5:59
TimeTrackerScript: [tick] 36:12    [CountdownThisSection] 5:54    [CountdownFull] 5:54
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 36:17    [CountdownThisSection] 5:49    [CountdownFull] 5:49
TimeTrackerScript: [tick] 36:22    [CountdownThisSection] 5:44    [CountdownFull] 5:44
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 36:27    [CountdownThisSection] 5:39    [CountdownFull] 5:39
TimeTrackerScript: [tick] 36:32    [CountdownThisSection] 5:34    [CountdownFull] 5:34
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 36:37    [CountdownThisSection] 5:30    [CountdownFull] 5:30
TimeTrackerScript: [tick] 36:42    [CountdownThisSection] 5:25    [CountdownFull] 5:25
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 36:47    [CountdownThisSection] 5:20    [CountdownFull] 5:20
TimeTrackerScript: [tick] 36:52    [CountdownThisSection] 5:15    [CountdownFull] 5:15
WWise_VO: Callback triggered: AK_MusicSyncUserCue
WWise_VO: Unexpected Cue: AK_MusicSyncUserCue | Cue_VoiceElicitation1_End
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 36:57    [CountdownThisSection] 5:10    [CountdownFull] 5:10
TimeTrackerScript: [tick] 37:02    [CountdownThisSection] 5:05    [CountdownFull] 5:05
WWise_VO: Callback triggered: AK_MusicSyncUserCue
Wwise_VO: Cue_ClosingGoodbye_Start
WWise_VO: Callback triggered: AK_MusicSyncUserCue
Wwise_VO: Cue_ClosingGoodbye_Start
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 37:07    [CountdownThisSection] 5:00    [CountdownFull] 5:00
TimeTrackerScript: [tick] 37:12    [CountdownThisSection] 4:55    [CountdownFull] 4:55
TimeTrackerScript: [tick] 37:17    [CountdownThisSection] 4:50    [CountdownFull] 4:50
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 37:22    [CountdownThisSection] 4:45    [CountdownFull] 4:45
[MicVoiceIngest] FAIL_OBSERVATION composite is now TRUE — flags: FAIL_AUDIO_CALLBACK_GAP_HIGH | micExitReason=unread_zero hzRolling=45.7 maxGapMs=42.67 imitone/callback=0.999 rawLockMisses=138 overflowDrops=0 dbMicSnap=-73.30 imitoneStateStallFrames=6993
[MicVoiceIngest] FAIL_OBSERVATION still TRUE after 0.25s — flags: FAIL_AUDIO_CALLBACK_GAP_HIGH | micExitReason=unread_zero hzRolling=45.7 maxGapMs=42.67 imitone/callback=0.999 rawLockMisses=138 overflowDrops=0 dbMicSnap=-71.69 imitoneStateStallFrames=7035
[MicVoiceIngest] FAIL_OBSERVATION still TRUE after 0.76s — flags: FAIL_AUDIO_CALLBACK_GAP_HIGH | micExitReason=unread_zero hzRolling=45.7 maxGapMs=42.67 imitone/callback=0.999 rawLockMisses=138 overflowDrops=0 dbMicSnap=-71.07 imitoneStateStallFrames=7118
[MicVoiceIngest] FAIL_OBSERVATION cleared after 1.01s — flags seen during window: FAIL_AUDIO_CALLBACK_GAP_HIGH 
TimeTrackerScript: [tick] 37:27    [CountdownThisSection] 4:40    [CountdownFull] 4:40
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 37:32    [CountdownThisSection] 4:35    [CountdownFull] 4:35
TimeTrackerScript: [tick] 37:37    [CountdownThisSection] 4:30    [CountdownFull] 4:30
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 37:42    [CountdownThisSection] 4:25    [CountdownFull] 4:25
TimeTrackerScript: [tick] 37:47    [CountdownThisSection] 4:20    [CountdownFull] 4:20
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 37:52    [CountdownThisSection] 4:15    [CountdownFull] 4:15
TimeTrackerScript: [tick] 37:57    [CountdownThisSection] 4:10    [CountdownFull] 4:10
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 38:02    [CountdownThisSection] 4:05    [CountdownFull] 4:05
TimeTrackerScript: [tick] 38:07    [CountdownThisSection] 4:00    [CountdownFull] 4:00
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 38:12    [CountdownThisSection] 3:55    [CountdownFull] 3:55
TimeTrackerScript: [tick] 38:17    [CountdownThisSection] 3:50    [CountdownFull] 3:50
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 38:22    [CountdownThisSection] 3:45    [CountdownFull] 3:45
TimeTrackerScript: [tick] 38:27    [CountdownThisSection] 3:40    [CountdownFull] 3:40
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 38:32    [CountdownThisSection] 3:35    [CountdownFull] 3:35
TimeTrackerScript: [tick] 38:37    [CountdownThisSection] 3:30    [CountdownFull] 3:30
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 38:42    [CountdownThisSection] 3:25    [CountdownFull] 3:25
TimeTrackerScript: [tick] 38:47    [CountdownThisSection] 3:20    [CountdownFull] 3:20
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 38:52    [CountdownThisSection] 3:15    [CountdownFull] 3:15
TimeTrackerScript: [tick] 38:57    [CountdownThisSection] 3:10    [CountdownFull] 3:10
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 39:02    [CountdownThisSection] 3:05    [CountdownFull] 3:05
TimeTrackerScript: [tick] 39:07    [CountdownThisSection] 3:01    [CountdownFull] 3:01
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 39:12    [CountdownThisSection] 2:56    [CountdownFull] 2:56
TimeTrackerScript: [tick] 39:17    [CountdownThisSection] 2:51    [CountdownFull] 2:51
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 39:22    [CountdownThisSection] 2:46    [CountdownFull] 2:46
TimeTrackerScript: [tick] 39:27    [CountdownThisSection] 2:41    [CountdownFull] 2:41
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 39:32    [CountdownThisSection] 2:36    [CountdownFull] 2:36
TimeTrackerScript: [tick] 39:37    [CountdownThisSection] 2:31    [CountdownFull] 2:31
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
[MicVoiceIngest] FAIL_OBSERVATION composite is now TRUE — flags: FAIL_AUDIO_CALLBACK_GAP_HIGH | micExitReason=unread_zero hzRolling=45.7 maxGapMs=42.83 imitone/callback=0.999 rawLockMisses=144 overflowDrops=0 dbMicSnap=-72.71 imitoneStateStallFrames=29105
[MicVoiceIngest] FAIL_OBSERVATION still TRUE after 0.25s — flags: FAIL_AUDIO_CALLBACK_GAP_HIGH | micExitReason=copied_samples hzRolling=45.7 maxGapMs=42.83 imitone/callback=0.999 rawLockMisses=144 overflowDrops=0 dbMicSnap=-72.12 imitoneStateStallFrames=29147
[MicVoiceIngest] FAIL_OBSERVATION still TRUE after 0.76s — flags: FAIL_AUDIO_CALLBACK_GAP_HIGH | micExitReason=unread_zero hzRolling=45.7 maxGapMs=42.83 imitone/callback=0.999 rawLockMisses=145 overflowDrops=0 dbMicSnap=-71.76 imitoneStateStallFrames=29230
[MicVoiceIngest] FAIL_OBSERVATION cleared after 1.01s — flags seen during window: FAIL_AUDIO_CALLBACK_GAP_HIGH 
TimeTrackerScript: [tick] 39:42    [CountdownThisSection] 2:26    [CountdownFull] 2:26
TimeTrackerScript: [tick] 39:47    [CountdownThisSection] 2:21    [CountdownFull] 2:21
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 39:52    [CountdownThisSection] 2:16    [CountdownFull] 2:16
TimeTrackerScript: [tick] 39:57    [CountdownThisSection] 2:11    [CountdownFull] 2:11
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 40:02    [CountdownThisSection] 2:06    [CountdownFull] 2:06
TimeTrackerScript: [tick] 40:07    [CountdownThisSection] 2:01    [CountdownFull] 2:01
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 40:12    [CountdownThisSection] 1:56    [CountdownFull] 1:56
TimeTrackerScript: [tick] 40:17    [CountdownThisSection] 1:51    [CountdownFull] 1:51
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 40:22    [CountdownThisSection] 1:46    [CountdownFull] 1:46
TimeTrackerScript: [tick] 40:27    [CountdownThisSection] 1:41    [CountdownFull] 1:41
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 40:32    [CountdownThisSection] 1:36    [CountdownFull] 1:36
TimeTrackerScript: [tick] 40:37    [CountdownThisSection] 1:31    [CountdownFull] 1:31
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 40:42    [CountdownThisSection] 1:26    [CountdownFull] 1:26
TimeTrackerScript: [tick] 40:47    [CountdownThisSection] 1:21    [CountdownFull] 1:21
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 40:52    [CountdownThisSection] 1:16    [CountdownFull] 1:16
TimeTrackerScript: [tick] 40:57    [CountdownThisSection] 1:11    [CountdownFull] 1:11
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 41:02    [CountdownThisSection] 1:06    [CountdownFull] 1:06
TimeTrackerScript: [tick] 41:07    [CountdownThisSection] 1:01    [CountdownFull] 1:01
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 41:12    [CountdownThisSection] 0:56    [CountdownFull] 0:56
TimeTrackerScript: [tick] 41:17    [CountdownThisSection] 0:51    [CountdownFull] 0:51
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 41:22    [CountdownThisSection] 0:46    [CountdownFull] 0:46
TimeTrackerScript: [tick] 41:27    [CountdownThisSection] 0:41    [CountdownFull] 0:41
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 41:32    [CountdownThisSection] 0:36    [CountdownFull] 0:36
TimeTrackerScript: [tick] 41:37    [CountdownThisSection] 0:31    [CountdownFull] 0:31
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 41:42    [CountdownThisSection] 0:26    [CountdownFull] 0:26
TimeTrackerScript: [tick] 41:47    [CountdownThisSection] 0:22    [CountdownFull] 0:22
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 41:52    [CountdownThisSection] 0:17    [CountdownFull] 0:17
TimeTrackerScript: [tick] 41:57    [CountdownThisSection] 0:12    [CountdownFull] 0:12
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 42:02    [CountdownThisSection] 0:07    [CountdownFull] 0:07
TimeTrackerScript: [tick] 42:07    [CountdownThisSection] 0:02    [CountdownFull] 0:02
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 42:12    [CountdownThisSection] 0:00    [CountdownFull] 0:00
TimeTrackerScript: [tick] 42:17    [CountdownThisSection] 0:00    [CountdownFull] 0:00
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 42:22    [CountdownThisSection] 0:00    [CountdownFull] 0:00
TimeTrackerScript: [tick] 42:27    [CountdownThisSection] 0:00    [CountdownFull] 0:00
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
TimeTrackerScript: [tick] 42:32    [CountdownThisSection] 0:00    [CountdownFull] 0:00
TimeTrackerScript: [tick] 42:37    [CountdownThisSection] 0:00    [CountdownFull] 0:00
DirectVoiceMonitoring Health: seeks(total=0, drift=0, start=0, switch=0) cooldownSuppressed=0 rebinds(success=0, fail=0) buffered(underflow=1, underflowSamples=1024, overflow=2, overflowSamples=1024, starvation=1) transitions(start=0, stop=0, atten=4, switch=0) hardSteps=0 source=Normalized enabled=True
WwiseUnity: Sound engine terminated successfully.
Setting up 10 worker threads for Enlighten.
LightControl: OnDestroy called - LightControl (or its GameObject) is being destroyed.
ArgumentNullException: Value cannot be null.
Parameter name: key
  at System.Collections.Generic.Dictionary`2[TKey,TValue].Remove (TKey key) [0x00008] in <834b2ded5dad441e8c7a4287897d63c7>:0 
  at AkRoomAwareObject.OnDestroy () [0x00000] in <a7f92b6e6b344124a55114df62c3b91d>:0 

ArgumentNullException: Value cannot be null.
Parameter name: key
  at System.Collections.Generic.Dictionary`2[TKey,TValue].Remove (TKey key) [0x00008] in <834b2ded5dad441e8c7a4287897d63c7>:0 
  at AkRoomAwareObject.OnDestroy () [0x00000] in <a7f92b6e6b344124a55114df62c3b91d>:0 

Memory Statistics:
[ALLOC_TEMP_TLS] TLS Allocator
  StackAllocators : 
    [ALLOC_TEMP_MAIN]
      Peak usage frame count: [0-1.0 KB]: 17378 frames, [1.0 KB-2.0 KB]: 11 frames, [2.0 KB-4.0 KB]: 2 frames, [8.0 KB-16.0 KB]: 398512 frames, [16.0 KB-32.0 KB]: 3 frames, [32.0 KB-64.0 KB]: 1 frames, [128.0 KB-256.0 KB]: 1 frames, [256.0 KB-0.5 MB]: 8 frames, [2.0 MB-4.0 MB]: 1 frames
      Initial Block Size 4.0 MB
      Current Block Size 4.0 MB
      Peak Allocated Bytes 2.1 MB
      Overflow Count 0
    [ALLOC_TEMP_Background Job.worker 11]
      Initial Block Size 32.0 KB
      Current Block Size 32.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_Loading.PreloadManager]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 120.9 KB
      Overflow Count 4
    [ALLOC_TEMP_Background Job.worker 3]
      Initial Block Size 32.0 KB
      Current Block Size 32.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_Background Job.worker 4]
      Initial Block Size 32.0 KB
      Current Block Size 32.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 16]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 23.7 KB
      Overflow Count 0
    [ALLOC_TEMP_Background Job.worker 7]
      Initial Block Size 32.0 KB
      Current Block Size 32.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_Background Job.worker 6]
      Initial Block Size 32.0 KB
      Current Block Size 32.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_Background Job.worker 12]
      Initial Block Size 32.0 KB
      Current Block Size 32.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_Background Job.worker 10]
      Initial Block Size 32.0 KB
      Current Block Size 32.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_Background Job.worker 2]
      Initial Block Size 32.0 KB
      Current Block Size 32.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_Background Job.worker 15]
      Initial Block Size 32.0 KB
      Current Block Size 32.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 5]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 34.7 KB
      Overflow Count 0
    [ALLOC_TEMP_Background Job.worker 8]
      Initial Block Size 32.0 KB
      Current Block Size 32.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_Background Job.worker 13]
      Initial Block Size 32.0 KB
      Current Block Size 32.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_AssetGarbageCollectorHelper] x 19
      Initial Block Size 64.0 KB
      Current Block Size 64.0 KB
      Peak Allocated Bytes 138 B
      Overflow Count 0
    [ALLOC_TEMP_Background Job.worker 0]
      Initial Block Size 32.0 KB
      Current Block Size 32.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 6]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 34.7 KB
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 8]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 33.8 KB
      Overflow Count 0
    [ALLOC_TEMP_HIDInput]
      Initial Block Size 64.0 KB
      Current Block Size 64.0 KB
      Peak Allocated Bytes 10.9 KB
      Overflow Count 0
    [ALLOC_TEMP_Background Job.worker 14]
      Initial Block Size 32.0 KB
      Current Block Size 32.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_BatchDeleteObjects]
      Initial Block Size 64.0 KB
      Current Block Size 64.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 11]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 29.9 KB
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 15]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 22.5 KB
      Overflow Count 0
    [ALLOC_TEMP_Background Job.worker 9]
      Initial Block Size 32.0 KB
      Current Block Size 32.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 0]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 33.4 KB
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 4]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 34.7 KB
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 14]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 33.4 KB
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 13]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 33.4 KB
      Overflow Count 0
    [ALLOC_TEMP_Background Job.worker 1]
      Initial Block Size 32.0 KB
      Current Block Size 32.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_EnlightenWorker] x 10
      Initial Block Size 64.0 KB
      Current Block Size 64.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 1]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 35.0 KB
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 3]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 35.0 KB
      Overflow Count 0
    [ALLOC_TEMP_Background Job.worker 5]
      Initial Block Size 32.0 KB
      Current Block Size 32.0 KB
      Peak Allocated Bytes 54 B
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 12]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 33.4 KB
      Overflow Count 0
    [ALLOC_TEMP_UnityGfxDeviceWorker]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 3.2 KB
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 2]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 35.0 KB
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 10]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 33.4 KB
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 7]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 34.7 KB
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 17]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 33.4 KB
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 9]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 33.4 KB
      Overflow Count 0
    [ALLOC_TEMP_Job.worker 18]
      Initial Block Size 256.0 KB
      Current Block Size 256.0 KB
      Peak Allocated Bytes 22.5 KB
      Overflow Count 0
    [ALLOC_TEMP_Loading.AsyncRead]
      Initial Block Size 64.0 KB
      Current Block Size 64.0 KB
      Peak Allocated Bytes 402 B
      Overflow Count 0
[ALLOC_DEFAULT] Dual Thread Allocator
  Peak main deferred allocation count 157
    [ALLOC_BUCKET]
      Large Block size 4.0 MB
      Used Block count 1
      Peak Allocated bytes 1.2 MB
    [ALLOC_DEFAULT_MAIN]
      Peak usage frame count: [16.0 MB-32.0 MB]: 415917 frames
      Requested Block Size 16.0 MB
      Peak Block count 2
      Peak Allocated memory 25.9 MB
      Peak Large allocation bytes 8.0 MB
    [ALLOC_DEFAULT_THREAD]
      Peak usage frame count: [16.0 MB-32.0 MB]: 415917 frames
      Requested Block Size 16.0 MB
      Peak Block count 1
      Peak Allocated memory 22.5 MB
      Peak Large allocation bytes 16.0 MB
[ALLOC_TEMP_JOB_1_FRAME]
  Initial Block Size 2.0 MB
  Used Block Count 0
  Overflow Count (too large) 0
  Overflow Count (full) 0
[ALLOC_TEMP_JOB_2_FRAMES]
  Initial Block Size 2.0 MB
  Used Block Count 0
  Overflow Count (too large) 0
  Overflow Count (full) 0
[ALLOC_TEMP_JOB_4_FRAMES (JobTemp)]
  Initial Block Size 2.0 MB
  Used Block Count 1
  Overflow Count (too large) 0
  Overflow Count (full) 0
[ALLOC_TEMP_JOB_ASYNC (Background)]
  Initial Block Size 1.0 MB
  Used Block Count 1
  Overflow Count (too large) 0
  Overflow Count (full) 0
[ALLOC_GFX] Dual Thread Allocator
  Peak main deferred allocation count 4
    [ALLOC_BUCKET]
      Large Block size 4.0 MB
      Used Block count 1
      Peak Allocated bytes 1.2 MB
    [ALLOC_GFX_MAIN]
      Peak usage frame count: [64.0 KB-128.0 KB]: 398603 frames, [128.0 KB-256.0 KB]: 15236 frames, [256.0 KB-0.5 MB]: 2077 frames, [0.5 MB-1.0 MB]: 1 frames
      Requested Block Size 16.0 MB
      Peak Block count 1
      Peak Allocated memory 0.6 MB
      Peak Large allocation bytes 0 B
    [ALLOC_GFX_THREAD]
      Peak usage frame count: [32.0 KB-64.0 KB]: 415917 frames
      Requested Block Size 16.0 MB
      Peak Block count 1
      Peak Allocated memory 46.6 KB
      Peak Large allocation bytes 0 B
[ALLOC_CACHEOBJECTS] Dual Thread Allocator
  Peak main deferred allocation count 2
    [ALLOC_BUCKET]
      Large Block size 4.0 MB
      Used Block count 1
      Peak Allocated bytes 1.2 MB
    [ALLOC_CACHEOBJECTS_MAIN]
      Peak usage frame count: [1.0 MB-2.0 MB]: 415917 frames
      Requested Block Size 4.0 MB
      Peak Block count 1
      Peak Allocated memory 1.3 MB
      Peak Large allocation bytes 0 B
    [ALLOC_CACHEOBJECTS_THREAD]
      Peak usage frame count: [1.0 MB-2.0 MB]: 415916 frames, [2.0 MB-4.0 MB]: 1 frames
      Requested Block Size 4.0 MB
      Peak Block count 1
      Peak Allocated memory 2.9 MB
      Peak Large allocation bytes 0 B
[ALLOC_TYPETREE] Dual Thread Allocator
  Peak main deferred allocation count 0
    [ALLOC_BUCKET]
      Large Block size 4.0 MB
      Used Block count 1
      Peak Allocated bytes 1.2 MB
    [ALLOC_TYPETREE_MAIN]
      Peak usage frame count: [4.0 KB-8.0 KB]: 415917 frames
      Requested Block Size 2.0 MB
      Peak Block count 1
      Peak Allocated memory 4.1 KB
      Peak Large allocation bytes 0 B
    [ALLOC_TYPETREE_THREAD]
      Peak usage frame count: [2.0 KB-4.0 KB]: 415917 frames
      Requested Block Size 2.0 MB
      Peak Block count 1
      Peak Allocated memory 2.9 KB
      Peak Large allocation bytes 0 B


## Notes from session

Add observations, timestamps, or line references as you review:

-Savasana: Why are the lights still on for a while when we enter Savasana?

-Did we ever change the soundscape or the fundamental in the playground? It doesn't sound like it.

-Breathwork seemed to be playing throughout the session. We need to make sure it isn't. Probably kill it at start of playground, and at start of Savasana

## Findings

Add notes here from debugging process.