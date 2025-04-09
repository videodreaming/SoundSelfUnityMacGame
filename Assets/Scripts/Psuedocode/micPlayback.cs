//This is pseudocode for the direct monitoring and playback of the audio from the microphone

public class MicPlayback : MonoBehaviour
{
    // Reference to the direct microphone input stream. (whatever that is in Unity)
    private  GameValues gameValues;
    private ImitoneVoiceInterpreter imitoneVoiceInterpreter
    private const float dbTarget = -10f; // We can set this manually, here, to find a good level for microphone reamplification.
    private const float dbUpRate = 0.25f; // The rate at which we want to increase the gain of the microphone input, per second
    private const float dbUpRateFast = 2f; // The rate at which we want to increase the gain of the microphone input, per second, when initializing or when we need to quickly increase it.
    private const float dbDownRate = 10f; // The rate at which we want to decrease the gain of the microphone input, per second
    private const float dbDownRateLimit = 50f; // The rate at which we want to decrease the gain of the microphone input, per second, when clipping is a concern
    private const float limitThreshold = 8f; // The threshold above which we consider the audio at risk of clipping
    private const float wayTooQuietThreshold = -15f; // The threshold below which the volume is unacceptably quiet.
    private const float toleranceOver = 2f; // The tolerance for the gain to be over the target level before we start to decrease the gain
    private const float toleranceUnder = -2f; // The tolerance for the gain to be under the target level before we start to increase the gain
    private float _gain = 0f; // The gain that will be used, in decibels, to amplify the microphone input.
    public float levelAmplification = 0f; //gain, as a multiplier, to amplify the microphone input.

    // VOCABULARY NOTE:
    // "GAIN" refers to change in decibels, which are logarithmic
    // "LEVEL" refers to the actual amplitude of the signal, which is linear.

    void Update()
    {
        NormalizeOverTime();
    }

    private void NormalizeOverTime()
    {
        // Normalize the audio input over time to prevent clipping and distortion, and to ensure a consistent volume level.
        // We do this by tracking the level of the audio input, and gradually adjusting the gain in order to amplify or attenuate the input signal to a target level.

        if(imitoneVoiceInterpreter.toneActive)
        {
            string lerpType;

            if (GetDecibels(audioInput.level) > dbTarget + limitThreshold)
            {
                // If the level is above the target level + limit threshold, decrease the gain quickly to prevent clipping
                _gain -= dbDownRateLimit * Time.deltaTime;
                lerpType = "down fast";
            }
            else if ((GetDecibels(audioInput.level) < dbTarget + wayTooQuietThreshold) && imitoneVoiceInterpreter._tThisTone > 1.0f)
            {
                // If the level is below the target level - way too quiet threshold, increase the gain quickly
                _gain += dbUpRateFast * Time.deltaTime;
                lerpType = "up fast";
            }
            else if (GetDecibels(audioInput.level) > dbTarget + tolerance.over)
            {
                // If the level is above the target level + tolerance, decrease the gain
                _gain -= dbDownRate * Time.deltaTime;
                lerpType = "down";
            }
            else if (GetDecibels(audioInput.level) < dbTarget + tolerance.under)
            {
                // If the level is below the target level - tolerance, increase the gain
                bool initializing = (imitoneVoiceInterpreter._tSessionToneActive < 30f); //increase it faster if we are initializing.
                _gain += (initializing ? dbUpRateFast : dbUpRate)  * Time.deltaTime;
                if(initializing)
                lerpType = "up fast";
                else
                lerpType = "up";
            }
            else
            {
                lerpType = "stable";
            }

            float _gameAdjust = gameValues._chantLerpFast * GetLevel(16f * gameValues._chantLerpSlow - 16f) * GetLevel(gameValues._chantCharge * -12f); // artistically adjust level based on game values.

            levelAmplification = GetLevel(_gain) * _gameAdjust; // Convert the gain in decibels to a linear level for amplification
            //REEF - THIS AMPLIFICATION SHOULD BE APPLIED TO THE SIGNAL BEFORE IT IS PROCESSED BY LORNA'S AUDIO CHAIN.

            //NOTES FOR IMPLEMENTATION OF LORNA'S SIGNAL CHAIN:
            //LIMITING: GIVEN THE NEW AMPLIFICATION THIS CREATES THE POSSIBILITY OF, LORNA SHOULD APPLY A LIMITER TO IT AS WELL TO AVOID CLIPPING. SHE WILL KNOW HOW TO DO THAT, PLEASE ASK HER.
            //_CHANTCHARGE ADJUST: IT WILL WORK WELL IF THE SOUND BEGINS WITH LESS REVERB AT THE BEGINNING OF A TONE, AND THEN GETS STEADILY MORE REVERB (TO USE AUDIO LANGUAGE, GOES FROM MORE "DRY" TO MORE "WET"). THIS CAN BE DONE USING THE GAMEVALUES._CHANTCHARGE VARIABLE. HIGHER _CHANTCHARGE SHOULD HAVE HIGHER REVERB SETTINGS

            if (imitoneVoiceInterpreter._tThisTone % 2f < Time.deltaTime)
            {
                Debug.Log($"[MIC PLAYBACK] [Input: {GetDecibels(audioInput.level):F2} dB]  [Gain: {_gain:F2} dB]  [Game Adjustment: {GetDecibels(_gameAdjust):F2} dB]  [{lerpType}]   ");
            }

        }
    }

    public float GetDecibels(float level)
    {
        // Convert the linear level to decibels using the formula: dB = 20 * log10(level)
        // We use Mathf.Log10 for the logarithm base 10.
        return 20f * Mathf.Log10(Mathf.Max(level, 0.00001f)); // Avoid log(0) by using a small value
    }

    public float GetLevel(float db)
    {
        // Convert decibels to linear level using the formula: level = 10^(dB / 20)
        return Mathf.Pow(10f, db * 0.05f); // 20 is used in the denominator for conversion to linear scale
    }
}