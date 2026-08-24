namespace CreateEnv
{
    // Tiny handoff between the StartScreen/Editor scene and the Explore scene.
    // The StartScreen sets Selected, loads Explore, and EnvironmentLoader reads it.
    public static class EnvironmentSession
    {
        public static EnvironmentProfile Selected;

        // Set by FreeExploreConfig's Edit button before loading the builder scene;
        // StartScreenUI consumes (and clears) it to open the editor pre-filled.
        public static string EditRequestId;
    }
}
