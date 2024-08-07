﻿using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Octokit;
using SS14.MaintainerBot.Github.Types;

namespace SS14.MaintainerBot.Github.Entities;

[PrimaryKey(nameof(PullRequestId), nameof(CommentId))]
public class PullRequestComment
{
    public Guid PullRequestId { get; set; }
    
    public long CommentId { get; set; }
    
    [Required]
    public PrCommentType CommentType { get; set; }
}