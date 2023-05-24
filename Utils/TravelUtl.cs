namespace RP.SOI.DotNet.Utils;

public static class TravelUtl
{
    public static string Abbreviate(this string story)
    {
        if (story.Length < 30)
        {
            return story;
        }
        else
        {
            return story[..20] + "...";
        }
    }
}
