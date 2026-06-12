-- Top 3 active step assignments per user, overdue first then soonest due date.
-- ISNULL on DueAt pushes null-deadline steps after steps with a known due date.
SELECT
    ranked.UserId,
    ranked.StepId,
    ranked.StepName,
    ranked.WorkflowInstanceId,
    ranked.WorkflowName,
    ranked.DueAt,
    ranked.IsOverdue
FROM (
    SELECT
        si.AssigneeId                        AS UserId,
        si.Id                                AS StepId,
        si.StepName,
        wi.Id                                AS WorkflowInstanceId,
        wi.WorkflowName,
        si.DueAt,
        si.IsOverdue,
        ROW_NUMBER() OVER (
            PARTITION BY si.AssigneeId
            ORDER BY si.IsOverdue DESC, ISNULL(si.DueAt, '9999-12-31') ASC
        )                                    AS RowNum
    FROM workflows.StepInstances si
    INNER JOIN workflows.WorkflowInstances wi
        ON  wi.Id        = si.WorkflowInstanceId
        AND wi.IsDeleted = 0
    INNER JOIN identity.Users u
        ON  u.Id        = si.AssigneeId
        AND u.TenantId  = @TenantId
        AND u.IsDeleted = 0
        AND u.IsActive  = 1
    WHERE si.TenantId  = @TenantId
      AND si.IsDeleted = 0
      AND si.Status IN ('Assigned', 'InProgress', 'Pending')
) ranked
WHERE ranked.RowNum <= 3
ORDER BY ranked.UserId, ranked.IsOverdue DESC, ISNULL(ranked.DueAt, '9999-12-31') ASC
