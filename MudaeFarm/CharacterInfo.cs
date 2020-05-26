namespace MudaeFarm
{
    public readonly struct CharacterInfo
    {
        public readonly string Name;
        public readonly string Anime;

        public CharacterInfo(string name, string anime)
        {
            Name  = name;
            Anime = anime;
        }
    }
}