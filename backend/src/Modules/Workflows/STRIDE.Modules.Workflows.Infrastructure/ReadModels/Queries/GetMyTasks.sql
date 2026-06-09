-- Part 1: Standard steps directly assigned to this user
SELECT
    si.Id              AS StepInstanceId,
    si.StepName,
    si.Status          AS StepStatus,
    si.AssignedAt,
    wi.Id              AS WorkflowInstanceId,
    wi.WorkflowName,
    wi.Status          AS WorkflowStatus,
    c.Name             AS ClientName,
    0                  AS IsApprovalTask
FROM   [workflows].[StepInstances]    si
JOIN   [workflows].[WorkflowInstances] wi ON wi.Id = si.WorkflowInstanceId
LEFT JOIN [clients].[Clients]         c  ON c.Id  = wi.ClientId
                                         AND c.IsDeleted = 0
WHERE  si.TenantId   = @TenantId
  AND  si.AssigneeId = @UserId
  AND  si.Status NOT IN ('Completed', 'Skipped', 'Failed', 'AwaitingApproval', 'Rejected')
  AND  wi.Status NOT IN ('Completed', 'Cancelled', 'Failed', 'Archived')
  AND  wi.IsDeleted  = 0

UNION ALL

-- Part 2: Approval-gate steps open to any user with the required role
SELECT
    si.Id              AS StepInstanceId,
    si.StepName,
    si.Status          AS StepStatus,
    ar.CreatedAt       AS AssignedAt,
    wi.Id              AS WorkflowInstanceId,
    wi.WorkflowName,
    wi.Status          AS WorkflowStatus,
    c.Name             AS ClientName,
    1                  AS IsApprovalTask
FROM   [workflows].[StepInstances]     si
JOIN   [workflows].[WorkflowInstances] wi ON wi.Id  = si.WorkflowInstanceId
JOIN   [workflows].[ApprovalRequests]  ar ON ar.StepInstanceId = si.Id
                                          AND ar.Status = 'Pending'
JOIN   [identity].[UserRoles]          ur ON ur.RoleId   = si.RequiredRoleId
                                          AND ur.UserId   = @UserId
                                          AND ur.TenantId = @TenantId
                                          AND ur.IsDeleted = 0
JOIN   [identity].[Users]              u  ON u.Id = ur.UserId
                                          AND u.IsActive  = 1
                                          AND u.IsDeleted = 0
LEFT JOIN [clients].[Clients]          c  ON c.Id = wi.ClientId
                                          AND c.IsDeleted = 0
WHERE  si.TenantId = @TenantId
  AND  si.Status   = 'AwaitingApproval'
  AND  wi.Status NOT IN ('Completed', 'Cancelled', 'Failed', 'Archived', 'Halted')
  AND  wi.IsDeleted = 0

ORDER BY AssignedAt DESC
