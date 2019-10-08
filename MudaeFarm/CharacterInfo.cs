namespace MudaeFarm
{
    public struct CharacterInfo
    {
        public string Name { get; }
        public string Anime { get; }

        public CharacterInfo(string name, string anime)
        {
            Name  = name;
            Anime = anime;
        }
    }
}