// // //Porting Imitone from Ozone to Unity
// // Before anything else, the audio being input from the microphone should be filtered, with a high-pass filter sit to 20 or 40 hz (40 if subwoofer is enabled). This is the "clean" audio, and is used elsewhere:
// // >>compressed and sent to master (see "preCompression" and "slowCompression")
// // >>sent to recorder for record/replay

// // //Then, before it is analyzed by imitone , it should be further high-passed at 40 hz or 120 hz (120hz if subwoofer is enabled). This is the "filtered" audio, and is only used by imitone.

// // //IMITONE SETTINGS
// // // Internal timing for the thresholds
// // delayAttack  = .050;	//how long it has to be above the trigger value (once) and the confirmation value, before it'll switch on.
// // delayRelease = .25;	//how long it has to be below the confirmation value before it will turn off. Was originally 0.005
// // delayGlide   = .002;	//for moving between resonators - should be low, this only comes into play if you've already activated a tone, and it's handing off. Not more than 1/3 of delayAttack / delayRelease. Not less than 0.002. 0.002 might just be a good value to stick with.


// // //METHOD FOR SETTING VOLUME THRESHOLDS:

// // //Formula for getting Decibel values out of raw amplitude, and vice-versa, these should be generalized and available elsewhere:

// // Formula Decibels(Input amplitude)
// // 	(20 * (10 LOG ([amplitude] MAX .00001)));
// // Formula Level(Input db)
// // 	(10 ^ ([db] * .05));


// //DEFINED VARIABLES:
// backgroundIsNotHarmonic //we will set this when harmonicity is in a "reasonable" level, and change the way some behaviors work when it's unreasonable.

// // //"EXPECT NOISE FLOOR"
// // Variable: expectNoiseFloor (we use this when we are programatically telling the system to reset the noise floor now. This is assigned programatically, or in "manual mode", in which all the automtic functions are frozen, and the practitioner manually presses a button to turn on "expectNoiseFloor" for 1 second1.)

// // Variable: expectNoiseFloorHoldPenalty
// // When we are holding "expectNoiseFloor" for a long time, from 10 seconds to 70 seconds, it gradually slows down various lerp rates from 2^0, to 2^ -6.

// // //========================
// // //VOLUME THRESHOLD BEHAVIOR
// // //========================
// // raiseGateUntilNoVoice: 
// // When "expectNoiseFloor" turns on, FIRST we raise the volume threshold until the voice turns off.
// // - by 5db per second until the first time the voice stops.
// // - 1 second after the voice is successfully "off", we stop raising the volume threshold in this particular way.
// // - In that one-second period, however, if the voice is detected again, we raise the threshold by 1db per second.

// Tolerance: (in db)
// under = 12 - 2 * backgroundIsNotHarmonic //if the input is this many dB below the gate, start lowering it.
// over = 6 + 6 * backgroundIsNotHarmonic //permit sounds this many db louder than the gate before increasing it.

// //IMPORTANT:
// Curve: slider (in db) //(this is the curved tracking that is used for the volume threshold, or "gate")
// (initialize at -80 db)
// {
// Input:
// 	(
// 		the higher of (last frame's output from this curve) and (imitone.level in decibels - tolerance.over)
// 		+ 
// 		(
// 			(15.5 + raiseGateUntilNoVoice)
// 			* expectNoiseFloor [basically if the raiseGateUntilNoVoice behavior is going on]
// 		)
// 	) 
// 	MIN [means the lesser value between before/after this operator]
// 	(imitone.level in decibels + tolerance.under)

// //Interpolation Rates if the noise floor is currently being manually set.
// Up linear rate : 2 db per "frame"
// Down linear rate: 0.125 db per "frame"
// Damp: 0


// //Interpolation Rates if we are in "manual mode" but the noise floor is NOT currently being manually set.
// Up linear rate : 0
// Down linear rate: 0
// Damp: 0

// //Interpolation Rates if we are in "expectNoiseFloor" mode (but not in "Manual Mode)
// Up: 0.075 db per "frame" * expectNoiseFloorHoldPenalty
// Down: 0.125 db per "frame" * expectNoiseFloorHoldPenalty
// Dammp: 0.005 * 2^ (-2.7), but only if we are going down.


// //Interpolation Rates for normal behavior
// Up: 2 db per "frame" * expectNoiseFloorHoldPenalty
// Down: if backgroundIsNotHarmonic, 0.0009375 db/frame, else 0.0003125 db/frame
// Damp: 0.005 * 2^ (-2.7), but only if we are going down.	
// }


// //DB VALUES USED FOR IMITONE INPUT
// sliderSafeDB				= (slider MAX -68) // keeps db in usable values. "MAX" is an operator, it means "take the maximum value of..."
// levelReleaseTriggerDB	= 

// 	(
// 		(If backgroundIsNotHarmonic, then sliderSafeDB - 35 + 34 * expectNoiseFloor)
// 		Else, sliderSafeDB - 12
// 	)



// //IMITONE INPUTS FOR VOLUME:
// levelAttackTrigger = Level(sliderSafeDB)
// levelReleaseTrigger = Level(levelReleaseTriggerDB)
// levelAttackConfirm =
// 	Level(levelReleaseTriggerDB * 0.334 + sliderSafeDB * 0666)
// levelReleaseConfirm = 
// 	Level(levelReleaseTriggerDB * 0.666 + sliderSafeDB * 0.334)

// // //========================
// // //HARMONICITY THRESHOLD BEHAVIOR
// // //========================

// // //Harmonicity is a measure how much harmonic content is in the sample. Harmonicity is, perhaps appropriately, a very "noisy" value. So we are measuring the "high" end of the noise and the "low" end of the noise, and using both those values to determine how imitone should treat various harmonicity values.

// //note on the below:  we are making it more dynamic when "noiseFloorMove" is on to help it get un-stuck from mis-aligning the noise-floor's harmonicity to someone's voice, which can happen in a long session, and is a problem because it gives us "backgroundIsNotHarmonic = 0" when there's no environmental problem.

// // //This part is a little confusing: When I made this curve, I used the same interpolation settings for both the "high" and "low" end of the noise. To get the correct behavior using the same interpolation settings, I multiplied the "low" input by -1, and then un-inverted it later.


// //Variables that we will use in harmonicity
// quietDB		= sliderSafeDB - 20 + 8*backgroundIsNotHarmonic
// quietLevel	= Level(quietDB)
// isQuiet		= imitone.level < quietLevel
// quietSwitch = turns on when isQuiet is TRUE. Turns off when imitone.level >= Level(sliderSafeDB)

// moveNoiseFloor = 
// (isQuiet && (switchQuiet has been TRUE for more than 1 second)) 
// || 
// (
// 	expectNoiseFloor 
// 	&& (Decibels(imitone.level) < (sliderSafeDB - 1))
// )

// // //Getting into the meat of harmonicity calculations

// // Curve: Range
// // Input low = imitone.harmonicity * -1
// // Input high = imitone.harmonicity * 1

// linear down = 
// if noise floor is being set manually, and we are in manual mode,
// 	0.02 * 0.125
// else if expectNoiseFloor
// 	(0.01 * expectNoiseFloorHoldPenalty * 2^ (-3 - subwooferEnabled)) * moveNoiseFloor
// else
// 	0.01 * 2^ 
// 	((
// 		((how long quietSwitch has been TRUE) * -2 - backgroundIsNotHarmonic) 
// 		MAX
// 		(-7 +2 * backgroundIsNotHarmonic)
// 	) - subwooferEnabled) * moveNoiseFloor
	
// linear up =
// if noise floor is being set manually, and we are in manual mode,
// 	0.02 * expectNoiseFloorHoldPenalty * 0.5
// else if expectNoiseFloor
// 	0.5 * moveNoiseFloor
// else 
// 	2^ 
// 	(
// 		((the amount of time quietSwitch has been TRUE) * -1
// 		- backgroundIsNotHarmonic)
// 		MAX 
// 		(-5 + 3 * backgroundIsNotHarmonic)
// 	)  * moveNoiseFloor
	
// // Low = Range.low * -1
// // High = range.high
// // Middle = (Low + High) / 2

// //Here's how we decide "backgroundIsNotHarmonic"
// backgroundIsNotHarmonic = ((High + 2^ (-1.7)) MIN 0.97) < (0.9 - 2^ (-1.7))


// // //IMITONE VALUES TO SET

// // harmAttackTrigger   =  (High + 2^ (-1.7)) MIN 0.97;
// // harmAttackConfirm   =  High;
// // harmReleaseConfirm  =  High;
// // harmReleaseTrigger  =  Middle;