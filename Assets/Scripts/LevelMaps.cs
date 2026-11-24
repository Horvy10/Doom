// Mapy levelov ako text. Jeden znak = jedna bunka (LevelBuilder.cell, predvolene 4 m).
//
//  #  stena                 .  podlaha              P  štart hráča
//  z  zombie (strieľa)      i  imp (ohnivé gule)    d  démon (útočí zblízka)
//  h  lekárnička            a  brnenie              b  náboje       s  náboje do brokovnice
//  S  brokovnica            C  guľomet
//  D  dvere (otvára sa E)   r/u/y  dvere na červený/modrý/žltý kľúč
//  R/U/Y  červený/modrý/žltý kľúč
//  E  východ z levelu       x  spawn bod pre survival
//
// Dvere musia mať steny na dvoch protiľahlých stranách (vľavo+vpravo alebo hore+dole).
// Všetky riadky jednej mapy musia mať rovnakú dĺžku.
public static class LevelMaps
{
    public static readonly string[][] CampaignMaps =
    {
        new[]
        {
            "###############",
            "#P..h..#......#",
            "#......#..z...#",
            "#..b...D......#",
            "#......#...z..#",
            "#......#......#",
            "####D######D###",
            "#.....#....z..#",
            "#..z..#...d...#",
            "#.....#..s....#",
            "#..S..#.......#",
            "#.....D...E...#",
            "###############",
        },
        new[]
        {
            "#################",
            "#P....#.........#",
            "#..b..#..i...i..#",
            "#.....D.........#",
            "#..h..#....R....#",
            "#.....#.........#",
            "###D#############",
            "#.......#.......#",
            "#..z.z..#...d...#",
            "#.......r.......#",
            "#...s...#...a...#",
            "#..S....#....E..#",
            "#################",
        },
        new[]
        {
            "###################",
            "#P..#.....i.......#",
            "#...D.............#",
            "#.h.#...z...z..b..#",
            "#####.............#",
            "#...#######D#######",
            "#.a.#.....#.......#",
            "#...#..i..#..d.d..#",
            "#...u.....D.......#",
            "#...#..s..#...R...#",
            "#.E.#.....#.......#",
            "#...#..z..###r#####",
            "#...#.....#.......#",
            "#...#..C..D..d.U..#",
            "#...#.....#..i....#",
            "###################",
        },
    };

    public static readonly string[] Arena =
    {
        "#################",
        "#x.............x#",
        "#...............#",
        "#..##.......##..#",
        "#..##...h...##..#",
        "#...............#",
        "#S......P......C#",
        "#...b.......s...#",
        "#..##.......##..#",
        "#..##...a...##..#",
        "#...............#",
        "#x......x......x#",
        "#################",
    };

    public static string[] Campaign(int level)
    {
        int i = (UnityEngine.Mathf.Max(level, 1) - 1) % CampaignMaps.Length;
        return CampaignMaps[i];
    }
}
