
namespace SS14.MaintainerBot.Discord.Types;

public class ReviewPostModel
{
    public string State { get; init; }
    public string ReviewerName { get; init; }
    public string ReviewMessage { get; init; }
    public string Link { get; init; }
    
    public ReviewPostModel(string state, string reviewerName, string reviewMessage, string link)
    {
        State = state;
        ReviewerName = reviewerName;
        ReviewMessage = reviewMessage;
        Link = link;
    }
}