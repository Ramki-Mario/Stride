-- Team performance leaderboard: per-member aggregates for all users who completed
-- at least one assigned step in the date range. Optionally filtered by role.
SELECT
    u.Id                          AS UserId,
    u.DisplayName,
    u.Email,
    (
        SELECT TOP 1 r2.Name
        FROM   [identity].[UserRoles] ur2
        INNER JOIN [identity].[Roles]  r2
            ON  r2.Id        = ur2.RoleId
            AND r2.TenantId  = @TenantId
            AND r2.IsDeleted = 0
        WHERE  ur2.UserId    = u.Id
          AND  ur2.TenantId  = @TenantId
          AND  ur2.IsDeleted = 0
        ORDER BY r2.Name
    )                             AS Role,
    SUM(CASE WHEN si.Status = 'Completed' THEN 1 ELSE 0 END)
                                  AS CompletedSteps,
    COUNT(DISTINCT CASE WHEN wi.Status = 'Completed' THEN wi.Id END)
                                  AS CompletedWorkflows,
    ISNULL(
        AVG(CASE
                WHEN si.Status = 'Completed' AND si.AssignedAt IS NOT NULL
                THEN CAST(DATEDIFF(MINUTE, si.AssignedAt, si.CompletedAt) AS FLOAT)
            END),
        0.0
    )                             AS AvgStepDurationMinutes,
    CASE
        WHEN SUM(CASE WHEN si.Status = 'Completed' THEN 1 ELSE 0 END) = 0
        THEN 0.0
        ELSE SUM(CASE
                    WHEN si.Status      = 'Completed'
                     AND si.DueAt       IS NOT NULL
                     AND si.CompletedAt > si.DueAt
                    THEN 1.0
                    ELSE 0.0
                 END)
             * 100.0
             / SUM(CASE WHEN si.Status = 'Completed' THEN 1 ELSE 0 END)
    END                           AS OverdueRate
FROM [identity].[Users] u
INNER JOIN [workflows].[StepInstances] si
    ON  si.AssigneeId = u.Id
    AND si.TenantId   = @TenantId
INNER JOIN [workflows].[WorkflowInstances] wi
    ON  wi.Id = si.WorkflowInstanceId
WHERE u.TenantId   = @TenantId
  AND u.IsDeleted  = 0
  AND si.CompletedAt >= @FromDate
  AND si.CompletedAt <= @ToDate
  AND (
      @RoleId IS NULL
      OR EXISTS (
          SELECT 1
          FROM   [identity].[UserRoles] ur
          WHERE  ur.UserId    = u.Id
            AND  ur.TenantId  = @TenantId
            AND  ur.RoleId    = @RoleId
            AND  ur.IsDeleted = 0
      )
  )
GROUP BY u.Id, u.DisplayName, u.Email
HAVING SUM(CASE WHEN si.Status = 'Completed' THEN 1 ELSE 0 END) >= 1
ORDER BY CompletedSteps DESC;
