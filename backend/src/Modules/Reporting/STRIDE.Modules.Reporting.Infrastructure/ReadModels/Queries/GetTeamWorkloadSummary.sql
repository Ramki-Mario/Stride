-- Team workload summary: one row per active tenant user with their active step count.
-- LEFT JOIN so users with 0 assignments are included and sorted to the bottom.
SELECT
    u.Id                                                              AS UserId,
    u.DisplayName,
    u.Email,
    COUNT(si.Id)                                                      AS ActiveStepCount,
    CAST(MAX(CASE WHEN si.IsOverdue = 1 THEN 1 ELSE 0 END) AS BIT)   AS HasOverdueSteps
FROM [identity].[Users] u
LEFT JOIN [workflows].[StepInstances] si
    ON  si.AssigneeId = u.Id
    AND si.TenantId   = @TenantId
    AND si.Status IN ('Assigned', 'InProgress', 'Pending')
WHERE u.TenantId  = @TenantId
  AND u.IsDeleted = 0
  AND u.IsActive  = 1
GROUP BY u.Id, u.DisplayName, u.Email
ORDER BY COUNT(si.Id) DESC, u.DisplayName ASC
