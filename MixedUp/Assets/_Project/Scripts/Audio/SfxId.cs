namespace MixedUp
{
    /// <summary>Every sound effect of the game. They are all synthesised by ProceduralAudio, so no audio files are needed.</summary>
    public enum SfxId
    {
        FootGrass, FootDirt, FootWood, FootStone, FootSnow, FootWater, FootMud,
        Jump, Land, Splash, Pickup, Pass, Deliver, Hurt, Death, Zap, Bounce, Whoosh,
        UiClick, UiHover, Heal, Push, Squeak, IceCrack, Collapse, Win, Lose, Hug, Crouch, Bell, Gust
    }

    public enum MusicId { Menu, Game }
}
