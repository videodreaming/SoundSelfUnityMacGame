namespace SoundSelf.Sequence
{
    /// <summary>Expected <see cref="SetMenuStageHandler.Enter"/> behavior per <see cref="StageVariant"/> (Test Runner + playtest contract).</summary>
    public enum SetMenuEnterKind
    {
        WelcomePreCalibration,
        AlbumChoice,
        PsInteractiveOrMusic,
        UndefinedStubAutoComplete,
    }

    /// <summary>Policy mirror of <see cref="SetMenuStageHandler"/> menu-variant Enter side effects (UI target + linear bed).</summary>
    public static class SetMenuStagePolicy
    {
        public readonly struct SetMenuEnterExpectation
        {
            public SetMenuEnterKind Kind { get; }
            public bool StartsLinearAmbientBed { get; }
            public bool CompletesImmediately { get; }

            public SetMenuEnterExpectation(SetMenuEnterKind kind, bool startsLinearAmbientBed, bool completesImmediately)
            {
                Kind = kind;
                StartsLinearAmbientBed = startsLinearAmbientBed;
                CompletesImmediately = completesImmediately;
            }
        }

        public static bool TryGetEnterExpectation(StageVariant variant, out SetMenuEnterExpectation expectation)
        {
            switch (variant)
            {
                case StageVariant.Menu_Welcome_PreCalibration:
                    expectation = new SetMenuEnterExpectation(SetMenuEnterKind.WelcomePreCalibration, startsLinearAmbientBed: true, completesImmediately: false);
                    return true;
                case StageVariant.Menu_AlbumChoice:
                    expectation = new SetMenuEnterExpectation(SetMenuEnterKind.AlbumChoice, startsLinearAmbientBed: true, completesImmediately: false);
                    return true;
                case StageVariant.Menu_Ps_InteractiveOrMusic:
                    expectation = new SetMenuEnterExpectation(SetMenuEnterKind.PsInteractiveOrMusic, startsLinearAmbientBed: false, completesImmediately: false);
                    return true;
                default:
                    expectation = new SetMenuEnterExpectation(SetMenuEnterKind.UndefinedStubAutoComplete, startsLinearAmbientBed: false, completesImmediately: true);
                    return true;
            }
        }
    }
}
