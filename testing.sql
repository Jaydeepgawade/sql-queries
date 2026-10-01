-- Run after installing Fab_Ims_EquityDashboard.sql in a TEST database.
-- All calls are read-only. Supply a real RM/position from your database.
EXEC dbo.Fab_Ims_EquityDashboard @GenDtFrm='202603', @GenDtTo='202606', @Prole=1;
EXEC dbo.Fab_Ims_EquityDashboard @GenDtFrm='202603', @GenDtTo='202603', @Prole=1;
EXEC dbo.Fab_Ims_EquityDashboard @GenDtFrm='202603', @GenDtTo='202606', @RmCode='REPLACE_WITH_RM', @Prole=2;
EXEC dbo.Fab_Ims_EquityDashboard @PositionName='REPLACE_WITH_POSITION', @Prole=4;
EXEC dbo.Fab_Ims_EquityDashboard @Prole=3; -- Only status 6 in every populated result.
EXEC dbo.Fab_Ims_EquityDashboard @Prole=5; -- Only status 4.
EXEC dbo.Fab_Ims_EquityDashboard @Prole=2, @PStatus=5; -- Cards retain 1 and 5; other results contain 5 only.
EXEC dbo.Fab_Ims_EquityDashboard @Prole=1, @RmCode='__NO_SUCH_RM__'; -- Zero totals, eight zero cards, empty charts/detail.
GO
-- Validation tests: each must throw, and must not return dashboard data.
BEGIN TRY
    EXEC dbo.Fab_Ims_EquityDashboard @Prole=0;
    THROW 51000, 'FAIL: invalid role was accepted.', 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() <> 50001 THROW;
END CATCH;
BEGIN TRY
    EXEC dbo.Fab_Ims_EquityDashboard @Prole=1, @GenDtFrm='202613';
    THROW 51000, 'FAIL: invalid quarter was accepted.', 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() <> 50002 THROW;
END CATCH;
BEGIN TRY
    EXEC dbo.Fab_Ims_EquityDashboard @Prole=1, @GenDtFrm='202606', @GenDtTo='202603';
    THROW 51000, 'FAIL: reversed range was accepted.', 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() <> 50003 THROW;
END CATCH;
GO
-- Independent baseline: compare totals against result 1 for Admin, same range.
-- NULL Status follows the original procedure's Draft convention.
WITH SourceRows AS
(
    SELECT EmployeeCode, IncQtr, Incentive, AdjIncentive, Status FROM dbo.EqRmBrkIncSumm
    UNION ALL
    SELECT EmployeeCode, IncQtr, Incentive, AdjIncentive, Status FROM dbo.EqRmNwSelfClntRevSumm
    UNION ALL
    SELECT EmployeeCode, IncQtr, Incentive, AdjIncentive, Status FROM dbo.EqMnExisClntIncRevSumm
    UNION ALL
    SELECT EmployeeCode, IncQtr, Incentive, AdjIncentive, Status FROM dbo.EqReactIncentiveSumm
)
SELECT COUNT_BIG(*) AS IncentiveRecordCount,
       SUM(Incentive) AS GrossIncentive,
       SUM(ISNULL(AdjIncentive,0)) AS AdjustmentAmt,
       SUM(Incentive + ISNULL(AdjIncentive,0)) AS NetIncentive
FROM SourceRows
WHERE IncQtr BETWEEN '202603' AND '202606'
  AND Incentive > 0 AND ISNULL(Status,0) IN (0,1,3);

-- Manual acceptance checks with known test data:
-- 1. Totals equal the sums of detail monetary columns and each chart's totals.
-- 2. Eight status rows always appear, including zeros; sequence is 0,1,2,4,6,3,5,7.
-- 3. Two incentive types for one RM in the SAME quarter/status: RecordCount=2, RMQuarterCount=1.
-- 4. One RM in TWO quarters at the SAME status: TotalRM=1, RMQuarterCount=2.
-- 5. One RM-quarter with different statuses appears in both status cards. Do not sum distinct counts.
-- 6. A status outside the role's scope returns zero totals and no detail; cards stay role-scoped.
-- 7. FileAdjAmt appears separately and is NOT added to NetIncentive (matches original SP).
-- 8. Expired NISM is flagged in detail; financial/reporting status stays numeric and unchanged.
