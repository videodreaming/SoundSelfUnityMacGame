//THIS IS PSUEDOCODE FOR THE MUSIC SYSTEM

//COMMENT BELOW LINE TO SEE PSEUDOCODE
//*
//Ultimately we are going to be controlling Fundamentals and Harmonies. I think that we should send WWise simple note on and note off commands, for each of the twelve notes in the octave, and for both the fundamental and the harmony.

//private int fundamentalNote; //this is for influencing logic of the currently sung tone, as we are biasing towards tones that are harmonious with the fundamental
//private Dictionary<int, (float ActivateTimer, bool Active, float ChangeFundamentalTimer)> NoteTracker = new Dictionary<int, (float ActivateTimer, bool Active, float ChangeFundamentalTimer)>(); //this is for tracking the inputs and controlling the fundamental
//private Dictionary<int, bool> Fundamentals = new Dictionary<int, bool>(); //these will control WWise
//private Dictionary<int, bool> Harmonies = new Dictionary<int, bool>(); //these will control WWise

//==== DETECTING THE CURRENT TONE====//
//We need to balance responsiveness on the one hand, with consistency on the other, as we don’t want lots of rapid changes, but we do want the music system to respond as close to immediately when a tone begins as possible.

//This will require some experimentation, but the way I would implement it at first is this:

//private bool musicToneActive;

//private float musicNoteInputRaw;
//private float musicNoteInput;

//Update()
//{
  //  _musicNoteInputRaw = imitoneVoiceInterpreter.note MOD 12; //you’ll have to do something more complex than this to accommodate for the wrap-around behavior, but this will do for explanatory purposes. Also, note that imitone outputs various values for the note / tone. We want the one that is smoothest / least-chaotic. Also, I know imitone discerns between "note" and "tone" but I don't understand how that works, so I am using arbitrary terminology

    //there will need to be logic for handling the sudden changes of tone at the beginning and end of a sustained vocalization, as there can be some chaos in those moments that we don’t want to interfere with the system.

    //logic that will convert the raw stream of notes into something musically usable:

    //first, filter out disharmonious tones
    //if  (Difference(_musicNoteInputRaw, fundamentalNote) < 1.5)
    //musicNoteInput = fundamentalNote //half-step (one semitone) is interpreted as being on the fundamental

    //else if (Difference(_musicNoteInputRaw, fundamentalNote + 6)) //tritone
    //musicNoteInput = fundamentalNote + 5; //or +7, whichever is closer. this will additionally bias the music system TOWARD the perfect 5th which is the most beautiful harmony. 

    //else
    //musicNoteInput = musicNoteInputRaw;
    //we will have logic for setting the fundamental to the disharmonious note if the player realllllly sustains on those disharmonious notes.

    //so by now, unless we have "filtered out" ugly tones and rounded them to pretty tones, we will still have a float value.

    //Now let's turn this into usable note controls, starting by evening out the chaoatic input stream.
    //First, set the threshold used below. The intention of this is to make sure that as soon as toneActive becomes true, we have a note that we can use.

    //float _noteTrackerThreshold;
    //int nextNote;
    
    //if (imitoneVoiceInterpreter.toneActive)
    //{
     //   _noteTrackerThreshold = imitoneVoiceInterpreter.toneActiveThreshold2;
    //}
    //else
    //{
      //  _noteTrackerThreshold = imitoneVoiceInterpreter.toneActiveThreshold / 4; //this number was arbitrary
    //}
    
    //if(imitoneActive)
    //{
      //  for (each of the 12 notes in the NoteTracker dictionary)
        //{
          //  if (Round(musicNoteInput) = [Note]) //the Dictionary key, i.e. one of the 12 notes
            //{
              //  NoteTracker[Note].ActivateTimer += Time.deltaTime;
                //if (NoteTracker[Note].ActivateTimer >= _noteTrackerThreshold)
                //{
                    //get in there early, before the tone is active, to predict the first note.
                  //  if(thisIsTheHighestActivateTimer) //REEF - meaning "this note's timer is higher than any of the other notes' timers"
                    //nextNote = Note; //REEF - because the purpose of this is to identify a note, even if toneActive hasn't switched on yet, this is "nextNote". If toneActive hasn't switched on yet, we want to be ready with a note as soon as it does switch on. So before toneActive switches on, nextNote identifies the note that will be used as soon as it does. If toneActive is already on, then nextNote will immediately be used as the note.
                //}
            //}
            //if(imitoneVoiceInterpreter.toneActive && nextNote == [NOTE])
            //{
              //  NoteTracker[Note].Active = true; //REEF - this means we send a note-on to WWise
                //DeactivateOtherNotesInThisDictionary(); //REEF - meaning that if the note is on in that other note, we will turn it off.
                //SetOtherActivateTimersToZero();

                //if(_noteTrackerThreshold[_noteTrackerThreshold].ActivateTimer >= _noteTrackerThreshold)
                //NoteTracker[Note].ChangeFundamentalTimer += Time.deltaTime; 
            //}
        //}
        //else if(!toneActiveConfident)
        //{
          //  NoteTracker[Note].ActivateTimer = 0;
            //NoteTracker[Note].Active = false;
        //}
    //}

    //==== NEW NOTES FOR REEF ====//

    //REEF - We have several note messages that we are sending to WWise:
    //FOR FUNDAMENTALS -
    //We START the fundamental LOOPING layer for the fundamental note as soon as the fundamental changes to that note
    //We STOP the fundamental LOOPING layer for the fundamental note as soon as the fundamental changes to another note (and another note starts)
    //We START the fundamental ONE-SHOT layer for the fundamental note when the player starts toning (toneActiveBiasTrue)
    //We STOP the fundamental ONE-SHOT layer for the fundamental note when the player stops toning (toneActiveBiasTrue)
    //FOR HARMONIES -
    //We START the harmony LOOPING layer for the harmony note as soon as the harmony changes to that note
    //We STOP the harmony LOOPING layer for the harmony note as soon as the harmony changes to another note (and another note starts)
    //We START the harmony ONE-SHOT layer for the harmony note when the player starts toning (toneActiveBiasTrue)
    //We STOP the harmony ONE-SHOT layer for the harmony note when the player stops toning (toneActiveBiasTrue)

    //REEF, We will send the reward thump to WWise once chantCharge reaches 1.0 (or perhaps chantCharge rises above 0.9, test it out, I don't remember if it's finicky to actually reach 1.0)

    //=================================================

    //Now, NoteTracker should have the most likely note that the player is singing, and it should work adequately responsively. We can use this to control the music system.

    //==== CONTROLLING THE HARMONIES ====//
    //WE HAVE THREE SEPARATE PROGRAMS FOR CONTROLLING THIS
    //1. When the player is toning all over the place, we simply follow the NoteTracker.
    //2. When the player is doing what we want them to, which is sustaining tones, we produce a note that harmonizes well with BOTH the fundamental AND the tracked note. It sounds really good when the tone you're singing is sandwiched BETWEEN two other tones, all of which is harmonious. Lorna can help with this. It should not be just one note per sung tone, but should progress through a series of at least two. At the bottom of this pseudocode is the scripts I used in ozone, which you can use as a start.
    //3. When the player has been JUST chanting one tone for a good long time, and it's the fundamental, we should do something else entirely, which is to play another layer of the music that goes really well with the harmony, and I will leave it to Lorna to decide how this works. But it should certainly be responsive to the player's toning. We need to talk about how this works, but you can make the other two systems first, and then we can add this third system later. This is something new that is coming to me now, and replaces the "progression" system from the previous version of SoundSelf [if we don't get this in by May 8, that's fine, it requires custom composition]
    //The transitions between these three systems should be quite forgiving, i.e. the first time we detect a teencie note outside of the current behavior, shouldn't immediately trigger another behavior. When you have values for this, and other game-feel numbers, that work for you, it would be helpful if you would put them somewhere convenient for me to fine-tune.

    //Whatever the program, we should control WWise by sending a NoteOn and a NoteOff for the appropriate harmony (and fundamental).

    //==== CONTROLLING THE FUNDAMENTALS ====//
    //We can change fundamental in a couple of ways:
    //1. Dynamically, forced by the program, as will happen in the tutorial sequence to match Jaya's voice.
    //2. When the ChangeFundamentalTimer of the NoteTracker has passed a certain threshold, then the fundamental should change to the tracked note, but not right now, it should change the next time the note is sung, so that it begins to transition at the beginning of toning, which will feel good.
    //3. If a person is duly committed to a nasty note (a tritone, which is +- 6 from the fundamental, or a single semitone above or below). we can switch to that as the fundamental.

    //In both cases, we send a note-on to WWise for the WWise.fundamental and the WWise.harmony when the note changes, and a note-off to the ones they are changing away from.

    //================================================================================================

    //==== STUFF FROM OZONE!!!! ====//
    //Harmonize Rule
    //Input +cycle.two= 	harmonyInput.countNew MOD 2; //so each consecutive tone produces a different harmony.
    //          TrackedNote - Fundamental    |   Harmony Played
    //HarmonizeRule 		("zero",	0, .cycle.two, 5, 7); //
    //HarmonizeRule 		("one", 	1, .cycle.two, 5, 7); 
    //HarmonizeRule 		("two", 	2, .cycle.two, 5, 7);
    //HarmonizeRule 		("three",	3, .cycle.two, 5, 7);
    //HarmonizeRule 		("four", 	4, .cycle.two, 7, 9);
    //HarmonizeRule 		("five", 	5, .cycle.two, 9, 12);
    //HarmonizeRule 		("six", 	6, .cycle.two, 9, 12);
    //HarmonizeRule 		("seven",	7, .cycle.two, 4, 12);
    //HarmonizeRule 		("eight",	8, .cycle.two, 4, 5);
    //HarmonizeRule 		("nine", 	9, .cycle.two, 4, 5);
    //HarmonizeRule 		("ten", 	10,.cycle.two, 7, 12);
    //HarmonizeRule 		("eleven",	11,.cycle.two, 5, 7);
    //FOR EXAMPLE, if I am toning two semitones above the fundamental, the note that will be played for the harmony will either be a perfect fifth or a perfect seventh, and they will consecutively change back and forth

//}


