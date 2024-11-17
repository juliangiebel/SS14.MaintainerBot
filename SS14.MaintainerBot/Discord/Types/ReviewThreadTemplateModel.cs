using Octokit;
using SS14.MaintainerBot.Core.Models.Entities;

namespace SS14.MaintainerBot.Discord.Types;

public class ReviewThreadTemplateModel
{
    public PullRequest PullRequest { get; init; }
    public ReviewThread ReviewThread { get; init; }
    public string PrAuthor => PullRequest.User.Login;
    public string Link => PullRequest.HtmlUrl;
    public long Number => PullRequest.Number;
    
    public ReviewThreadTemplateModel(PullRequest pullRequest, ReviewThread reviewThread)
    {
        PullRequest = pullRequest;
        ReviewThread = reviewThread;
    }
}