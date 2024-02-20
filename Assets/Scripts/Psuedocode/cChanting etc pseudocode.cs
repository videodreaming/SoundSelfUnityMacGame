// //HOW CCHANTING WORKS
// When ToneActiveDuration > 0.2, set local variable "var" to TRUE
// When ToneNotActiveDuration > 0.33, set local variable "var" to FALSE

// //There needs to be a public variable called "Responsiveness" that we can edit, and will edit once we create the "Absorption" measurement, which is one of our clinical measurements.

// //When we create Cadence, we will be tracking a number of recent tones and breaks, and we will need a public variable "RecentToneMeanDuration"

// //In Ozone, "MAX" is an operator that outputs the higher of two values, and MIN does the opposite.

// Set variables:
// varDamp = 0.04 * 2^responsiveness / ((RecentToneMeanDuration MAX 0.25) MIN 10)

// //A series of curves to lerp the data
// //cChanting
// Curve
// Input = var
// damp = 0.08
// linear down = 0.08 

// Curve: cChanting
// Input = [LAST]
// damp = varDamp

// //cChantingFast
// Curve
// Input = var
// damp	= 0.16
// linear down	= 0.16

// Curve: cChantingFast
// Input = [LAST]
// damp = varDamp * 2

// //HOW CHANTCHARGE WORKS
// //These three curves all have the same damp values

// Curve: named "lerpedMemory"
// Input = RecentToneMeanDuration = 5 for the time being

// Curve: fullValue
// Input = If (We are in the Guided Vocalization sequence), then 5, else Mean(lerpedMemory, 10). 
//however, we should only be updating the input... not the lerping behavior but the input that is being lerped... when ToneActive is 0

// Curve: chantChargeCurve
// Input = 
// (
// 	((tThisTone / (fullValue MAX 1)) MIN 1)
// )

// //The lerp values:
// damp = 0.01 + 0.0125 * breathVolume
// damp2 = 0.05 //remember, this means that the *target* is lerped

// ChantCharge is a float that is always lerping between and 0 1. Clamp this at the end of everything

// There are also 3 other floats, 1 called lerped memory, full value, chantChargeCurve, and chantCharge
// all floats are used to derive ChantCharge.

// lerpedmemory1 should always be lerping from currentvalue (lerped memory initialized at 0), target value (recentToneMeanDuration) at the rate of damp2 = 0.05
// then take lerpedmemory1 and lerping from (currentValue) to targetvalue (lerpedmemory1) at the rate of damp = 0.01 + 0.0125 * breathVolume

// and then get full value1, full value is 5 if we are guided Vocalization sequence but else its is average(lerpedmemory2,10), then lerp (currentValue = 1f, targetvalue is fullvalue2)
//at a rate of damp2 = 0.05
// thentake full value1, and then lerp it so current value = currentValue to (fullvalue2) at the rate of damp = 0.01 + 0.0125 * breathVolume

// then chantChargeCurve1 = lerp(currentValue, ((tThisTone/(fullValue2 MAX 1)) MIN 1), rate of damp2 = 0.05)
// then chantChargeCurve2 = lerp(currentValue, chantChargeCurve1, 0.01 + 0.0125 * breathVolume) 

// then ChantChargeCurve2 * cChanting^4




// //This is the global variable for chantCharge:
// ChantCharge = chantChargeCurve * cChanting^4

// //HOW TO TELL IF WE HAVE PASSED A VOICE-TEST
// While the test is going on (i.e. 1.5 seconds has passed since the instruction), chantCharge > 0.75

// That's a pass.

// Wait until breathStage >= 2 to "win" and proceed, though.

// //HOW TO TELL IF WE HAVE FAILED A VOICE-TEST

// failTimer: measures how long _tThisRest has cumulatively been over 1 since the start of the test. 

// failThreshold = If this is the first consecutive failure, 15, else 20.

// When (failTimer > failThreshold) AND (_tThisRest > 3) AND we haven't met the pass condition on the voice-test yet 
//(so you can't pass, and then fail in the few frames before breathStage ?= 2)

