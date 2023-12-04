// //Porting Imitone from Ozone to Unity
// Before anything else, the audio being input from the microphone should be filtered, with a high-pass filter sit to 20 or 40 hz (40 if subwoofer is enabled). This is the "clean" audio, and is used elsewhere:
// >>compressed and sent to master (see "preCompression" and "slowCompression")
// >>sent to recorder for record/replay

// //Then, before it is analyzed by imitone , it should be further high-passed at 40 hz or 120 hz (120hz if subwoofer is enabled). This is the "filtered" audio, and is only used by imitone.

// //IMITONE SETTINGS
// // Internal timing for the thresholds
// delayAttack  = .050;	//how long it has to be above the trigger value (once) and the confirmation value, before it'll switch on.
// delayRelease = .25;	//how long it has to be below the confirmation value before it will turn off. Was originally 0.005
// delayGlide   = .002;	//for moving between resonators - should be low, this only comes into play if you've already activated a tone, and it's handing off. Not more than 1/3 of delayAttack / delayRelease. Not less than 0.002. 0.002 might just be a good value to stick with.


// //METHOD FOR SETTING VOLUME THRESHOLDS:

// //Formula for getting Decibel values out of raw amplitude, and vice-versa, these should be generalized and available elsewhere:

// Formula Decibels(Input amplitude)
// 	(20 * (10 LOG ([amplitude] MAX .00001)));
// Formula Level(Input db)
// 	(10 ^ ([db] * .05));


// //DEFINED VARIABLES:
// harmonicityIsReasonable //we will set this when harmonicity is in a "reasonable" level, and change the way some behaviors work when it's unreasonable.

// //"EXPECT NOISE FLOOR"
// Variable: expectNoiseFloor (we use this when we are manually telling the system to reset the noise floor now. This is assigned programatically, or in "manual mode", in which all the automtic functions are frozen, and the practitioner manually presses a button to turn on "expectNoiseFloor" for 1 second1.)

// Variable: expectNoiseFloorHoldPenalty
// When we are holding "expectNoiseFloor" for a long time, from 10 seconds to 70 seconds, it gradually slows down the linearLerpRate from 2^0, to 2^ -6.


// //VOLUME THRESHOLD BEHAVIOR
// raiseGateUntilNoVoice: 
// When "expectNoiseFloor" turns on, FIRST we raise the volume threshold until the voice turns off.
// - by 5db per second until the first time the voice stops.
// - 1 second after the voice is successfully "off", we stop raising the volume threshold in this particular way.
// - In that one-second period, however, if the voice is detected again, we raise the threshold by 1db per second.

// Tolerance: (in db)
// under = 12 - 2 * harmonicityIsReasonable //if the input is this many dB below the gate, start lowering it.
// over = 6 + 6 * harmonicityIsReasonable //permit sounds this many db louder than the gate before increasing it.

// //IMPORTANT:
// Curve: slider (in db) //(this is the curved tracking that is used for the volume threshold, or "gate")
// (initialize at -80 db)
// {
// Input:
// 	(
// 		the higher of (last frame's output from this curve) and (imitone.level in decib els - tolerance.over)
// 		+ 
// 		(
// 			(15.5 + raiseGateUntilNoVoice)
// 			* expectNoiseFloor
// 		)
// 	) 
// 	take the lesser value...
// 	(imitone.level in decibels + tolerance.under)
	
// Up linear rate:
// 	If the noise floor is being manually forced (and we are in manual mode), 2db per "frame" 
// 	Otherwise, if expectNoiseFloor, 0.075 db per "frame" * expectNoiseFloorHoldPenalty.
// 	Otherwise, 2 db per "frame"
	
// Down linear rate:
// 	If the noise floor is being manually forced (and we are in manual mode), 0.125 db per "frame"
// 	Otherwise, if expectNoiseFloor, 0.125 db per "frame" * expectNoiseFloorHoldPenalty.
// 	Otherwise, if harmonicityIsReasonable, 0.0009375 db/frame
// 	Otherwise, 0.0003125 db/frame
	
// Damp rates:
// 	If we are in manual mode, then 0.
// 	Else, if this curve is going down, then 0.005 * 2 ^ (-2.7)
// }


// //DB VALUES USED FOR IMITONE INPUT
// sliderSafe				= (slider MAX -68) // keeps db in usable values. "MAX" is an operator, it means "take the maximum value of..."
// levelReleaseTriggerDB	= 

// 	(
// 		(If harmonicityIsResonable, then sliderSafe - 1 - 34 * expectNoiseFloor)
// 		Else, sliderSafe - 12
// 	) * 0.334



// //IMITONE INPUTS FOR VOLUME:
// levelAttackTrigger = Level(sliderSafe)
// levelReleaseTrigger = Level(levelReleaseTriggerDB)
// levelAttackConfirm =
// 	Level(levelReleaseTriggerDB * 0.334 + sliderSafe * 0666)
// levelReleaseConfirm = 
// 	Level(levelReleaseTriggerDB * 0.666 + sliderSafe * 0.334)
