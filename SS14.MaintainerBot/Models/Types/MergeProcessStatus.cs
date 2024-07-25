namespace SS14.MaintainerBot.Models.Types;

public enum MergeProcessStatus
{
    /// <summary>
    /// Merging the PR is scheduled and it'll be merged once the merge delay has passed
    /// </summary>
    Scheduled,
    
    /// <summary>
    /// An api request for merging the PR has been made and merging is in process (direct or queued)
    /// </summary>
    Merging,
    
    /// <summary>
    /// Pull request is merged and the process is ready to get cleaned up and removed
    /// </summary>
    Merged,

    /// <summary>
    /// Merge process has been interrupted by conflicts or a maintainer
    /// </summary>
    Interrupted,
    
    /// <summary>
    /// Merge process failed and manual merging by a maintainer is required
    /// </summary>
    Failed,
    
    /// <summary>
    /// The Pull request has been closed and the process is ready to get cleaned up and removed
    /// </summary>
    Closed
}