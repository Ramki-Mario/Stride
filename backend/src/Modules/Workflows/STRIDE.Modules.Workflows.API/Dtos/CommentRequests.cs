namespace STRIDE.Modules.Workflows.API.Dtos;

/// <summary>Body for POST /api/workflows/instances/{id}/comments</summary>
public sealed record CreateCommentRequest(string Body);

/// <summary>Body for PUT /api/workflows/instances/{id}/comments/{commentId}</summary>
public sealed record EditCommentRequest(string NewBody);
