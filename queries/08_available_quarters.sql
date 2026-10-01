-- स्वतंत्र read-only query: role बदलून चालवा.
DECLARE @Role SMALLINT=1;
WITH SourceRows AS
(
    SELECT IncQtr, Incentive, ISNULL(Status,0) AS StatusCode FROM dbo.EqRmBrkIncSumm
    UNION ALL
    SELECT IncQtr, Incentive, ISNULL(Status,0) FROM dbo.EqRmNwSelfClntRevSumm
    UNION ALL
    SELECT IncQtr, Incentive, ISNULL(Status,0) FROM dbo.EqMnExisClntIncRevSumm
    UNION ALL
    SELECT IncQtr, Incentive, ISNULL(Status,0) FROM dbo.EqReactIncentiveSumm
)
SELECT DISTINCT IncQtr AS Quarter
FROM SourceRows
WHERE Incentive > 0
  AND LEN(IncQtr) = 6
  AND IncQtr COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'
  AND TRY_CONVERT(DATE, IncQtr + '01',112) IS NOT NULL
  AND ((@Role=1 AND StatusCode IN (0,1,3))
    OR (@Role=2 AND StatusCode IN (1,5))
    OR (@Role=3 AND StatusCode=6)
    OR (@Role=4 AND StatusCode IN (2,7))
    OR (@Role=5 AND StatusCode=4))
ORDER BY IncQtr;
