using System.Linq;

public class Harmony
{
    private readonly Note[] _notes;

    public Harmony(Note[] notes)
    {
        _notes = notes;
    }

    public void CheckDissonance(Note newNote)
    {
        if (newNote.CheckDissonantTo(this))
        {
            End();
        }
    }

    public void End(Note[] notesInNewHarmony = null)
    {
        foreach (Note note in _notes)
        {
            //doesn't end fundamental as long as fundamental is part of the harmony
            if (notesInNewHarmony != null && notesInNewHarmony.Contains(note)) continue;
            note.End();
        }
    }
}