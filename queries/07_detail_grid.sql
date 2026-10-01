-- स्वतंत्र SELECT query: SSMS मध्ये ही पूर्ण फाइल चालवा.
-- खालील filters बदला. NULL म्हणजे सर्व उपलब्ध values.
DECLARE @GenDtFrm VARCHAR(30)='202603', @GenDtTo VARCHAR(30)='202606',
        @RmCode VARCHAR(20)=NULL, @PRmFrm VARCHAR(20)=NULL,
        @PRmTo VARCHAR(20)=NULL, @PositionName VARCHAR(100)=NULL,
        @PStatus SMALLINT=NULL, @Prole SMALLINT=1;
SET NOCOUNT ON;
DROP TABLE IF EXISTS #Scope;
DROP TABLE IF EXISTS #Selected;
DROP TABLE IF EXISTS #Nism;
    SELECT @GenDtFrm = NULLIF(LTRIM(RTRIM(@GenDtFrm)), ''),
           @GenDtTo = NULLIF(LTRIM(RTRIM(@GenDtTo)), ''),
           @RmCode = NULLIF(LTRIM(RTRIM(@RmCode)), ''),
           @PRmFrm = NULLIF(LTRIM(RTRIM(@PRmFrm)), ''),
           @PRmTo = NULLIF(LTRIM(RTRIM(@PRmTo)), ''),
           @PositionName = NULLIF(LTRIM(RTRIM(@PositionName)), '');

    -- Match the previous upper-bound sentinel; use NULL in new callers.
    IF @GenDtTo = 'zzzzz' SET @GenDtTo = NULL;
    IF @PRmTo = 'zzzzz' SET @PRmTo = NULL;

    IF @Prole IS NULL OR @Prole NOT IN (1,2,3,4,5)
        THROW 50001, 'A valid authenticated role (1-5) is required.', 1;

    IF (@GenDtFrm IS NOT NULL AND
        (LEN(@GenDtFrm) <> 6 OR @GenDtFrm COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9]%'
         OR TRY_CONVERT(DATE, @GenDtFrm + '01',112) IS NULL))
       OR (@GenDtTo IS NOT NULL AND
        (LEN(@GenDtTo) <> 6 OR @GenDtTo COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9]%'
         OR TRY_CONVERT(DATE, @GenDtTo + '01',112) IS NULL))
        THROW 50002, 'Quarter must be a valid YYYYMM key.', 1;

    IF @GenDtFrm > @GenDtTo OR @PRmFrm > @PRmTo
        THROW 50003, 'From value must not exceed To value.', 1;
    IF @PStatus IS NOT NULL AND @PStatus NOT BETWEEN 0 AND 7
        THROW 50004, 'Status must be NULL or 0-7.', 1;

    DECLARE @StatusMap TABLE
    (
        StatusCode SMALLINT PRIMARY KEY,
        StatusName VARCHAR(100),
        FlowOrder INT,
        IsRejected BIT
    );
    INSERT INTO @StatusMap VALUES
        (0,'Draft',1,0),
        (1,'Pending Verifier',2,0),
        (2,'Pending Accountant',3,0),
        (4,'Pending HR',4,0),
        (6,'Completed',5,0),
        (3,'Rejected by Verifier - Pending Admin',6,1),
        (5,'Rejected by Accountant - Pending Verifier',7,1),
        (7,'Rejected by HR - Pending Accountant',8,1);

    -- Filter each row by role. Employee-level eligibility must not expose
    -- another incentive type/status belonging to the same RM.
    SELECT K.EmployeeCode, K.Name, K.PositionName, K.IncQtr,
           K.IncentiveType, K.Incentive, K.AdjIncentive, K.FileAdjAmt,
           K.Incentive + K.AdjIncentive AS NetIncentive,
           K.StatusCode, K.FilePath, K.UpdatedBy, K.Remarks
    INTO #Scope
    FROM
    (
        SELECT EmployeeCode, Name, PositionName, IncQtr, 'B' AS IncentiveType,
               Incentive, ISNULL(AdjIncentive,0) AS AdjIncentive,
               ISNULL(FileAdjAmt,0) AS FileAdjAmt, ISNULL(Status,0) AS StatusCode,
               FilePath, UpdatedBy, Remarks
        FROM dbo.EqRmBrkIncSumm
        UNION ALL
        SELECT EmployeeCode, Name, PositionName, IncQtr, 'S',
               Incentive, ISNULL(AdjIncentive,0), 0, ISNULL(Status,0),
               FilePath, UpdatedBy, Remarks
        FROM dbo.EqRmNwSelfClntRevSumm
        UNION ALL
        SELECT EmployeeCode, Name, PositionName, IncQtr, 'E',
               Incentive, ISNULL(AdjIncentive,0), 0, ISNULL(Status,0),
               FilePath, UpdatedBy, Remarks
        FROM dbo.EqMnExisClntIncRevSumm
        UNION ALL
        SELECT EmployeeCode, Name, PositionName, IncQtr, 'R',
               Incentive, ISNULL(AdjIncentive,0), 0, ISNULL(Status,0),
               FilePath, UpdatedBy, Remarks
        FROM dbo.EqReactIncentiveSumm
    ) K
    WHERE K.Incentive > 0
      AND (@GenDtFrm IS NULL OR K.IncQtr >= @GenDtFrm)
      AND (@GenDtTo IS NULL OR K.IncQtr <= @GenDtTo)
      AND (@RmCode IS NULL OR K.EmployeeCode = @RmCode)
      AND (@PRmFrm IS NULL OR K.EmployeeCode >= @PRmFrm)
      AND (@PRmTo IS NULL OR K.EmployeeCode <= @PRmTo)
      AND (@PositionName IS NULL OR K.PositionName = @PositionName)
      AND ((@Prole=1 AND K.StatusCode IN (0,1,3))
        OR (@Prole=2 AND K.StatusCode IN (1,5))
        OR (@Prole=3 AND K.StatusCode=6)
        OR (@Prole=4 AND K.StatusCode IN (2,7))
        OR (@Prole=5 AND K.StatusCode=4))
    OPTION (RECOMPILE);

    SELECT * INTO #Selected FROM #Scope
    WHERE @PStatus IS NULL OR StatusCode = @PStatus;

    -- Evaluate validity once per distinct selected RM.
    SELECT EmployeeCode, dbo.fn_ChkNISMvalidity(EmployeeCode) AS NismValid
    INTO #Nism
    FROM (SELECT DISTINCT EmployeeCode FROM #Selected) R;

    -- 7. Detail grid. btnFlag indicates source-state eligibility only.
    -- The action SP and server-side authorization must revalidate the action.
    SELECT S.IncQtr AS Quarter, S.EmployeeCode AS RmCode, S.Name AS RmName,
           S.PositionName, S.IncentiveType, S.Incentive AS GrossIncentive,
           S.AdjIncentive AS AdjustmentAmt, S.FileAdjAmt, S.NetIncentive,
           S.StatusCode, M.StatusName, N.NismValid,
           CASE WHEN N.NismValid=0 THEN 'NISM Expired'
                WHEN N.NismValid=1 THEN 'Valid' ELSE 'Unknown' END AS NismStatus,
           S.FilePath, S.UpdatedBy, S.Remarks,
           S.NetIncentive * 0.5 AS YearHoldAmt,
           S.NetIncentive * 0.5 AS FinPay,
           CASE WHEN N.NismValid=1 AND
                ((@Prole=1 AND S.StatusCode IN (0,3))
                 OR (@Prole=2 AND S.StatusCode IN (1,5))
                 OR (@Prole=4 AND S.StatusCode IN (2,7))
                 OR (@Prole=5 AND S.StatusCode=4))
                THEN 'Y' ELSE 'N' END AS btnFlag
    FROM #Selected S
    INNER JOIN @StatusMap M ON M.StatusCode=S.StatusCode
    LEFT JOIN #Nism N ON N.EmployeeCode=S.EmployeeCode
    ORDER BY S.IncQtr, S.EmployeeCode, S.IncentiveType, S.StatusCode;