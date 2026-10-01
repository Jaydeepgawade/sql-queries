# Equity Audit Dashboard

## स्वतंत्र queries कशा वापरायच्या?

queries folder मधील प्रत्येक फाइल स्वतंत्रपणे SSMS मध्ये चालवता येते. SP install करण्याची गरज नाही. मूळ चार tables आणि fn_ChkNISMvalidity function database मध्ये हवेत. प्रत्येक फाइलच्या सुरुवातीला quarter, RM, position, status आणि role filters आहेत.

| फाइल | उपयोग |
|---|---|
| 01_totals.sql | Gross, Adjustment, Net आणि payment cards |
| 02_status_cards.sql | Draft → Verifier → Accountant → HR → Completed आणि rejected counts |
| 03_quarter_summary.sql | Quarter-wise chart |
| 04_position_summary.sql | Position-wise chart |
| 05_rm_summary.sql | प्रत्येक RM चा quarter-wise incentive |
| 06_incentive_types.sql | B/S/E/R incentive summary |
| 07_detail_grid.sql | मूळ records, status आणि NISM माहिती |
| 08_available_quarters.sql | Role नुसार उपलब्ध quarters |

DECLARE मधील @Prole: 1 Admin (0,1,3), 2 Verifier (1,5), 3 Auditor (6), 4 Accountant (2,7), 5 HR (4). Role नुसार records मर्यादित आहेत. सर्व आठ cards येतात, पण त्या role ला न दिसणाऱ्या statuses चे counts शून्य असतात.

@GenDtFrm='202603', @GenDtTo='202606' म्हणजे त्या YYYYMM keys मधील range. Database मध्ये वापरलेली कोणतीही valid YYYYMM key निवडता येते. @RmCode=NULL म्हणजे सर्व RMs; एका RM साठी actual code द्या. @PositionName=NULL म्हणजे सर्व positions. @PStatus=NULL म्हणजे role ला उपलब्ध सर्व statuses. Status cards मात्र @PStatus ignore करतात.

प्रत्येक query मध्ये एकच SELECT result येतो. समान filters आणि चार tables एकत्र करण्याचा setup प्रत्येक फाइलमध्ये आहे, त्यामुळे कोणतीही फाइल एकटी चालते. हे learning/testing साठी आहे. Website वर सात queries स्वतंत्र चालवल्यास source data सात वेळा वाचला जाईल; एका request मध्ये सर्व results हवे असतील तर SP वापरा.

Count: IncentiveRecordCount म्हणजे incentive rows; TotalRM म्हणजे unique RM; RMQuarterCount म्हणजे unique RM-quarter pairs. वेगवेगळ्या statuses मध्ये तोच RM दिसू शकतो, त्यामुळे distinct counts ची बेरीज करू नका.

Net = Incentive + AdjIncentive. FileAdjAmt स्वतंत्र आहे. Queries read-only आहेत; Approve/Reject करत नाहीत.

SQL Server येथे उपलब्ध नसल्यामुळे actual database execution तपासलेले नाही. प्रथम test database मध्ये चालवा. SQL Server 2016 SP1+; मूळ schema/column types पडताळा.

## REST API

.NET 8 API, interfaces, service layer, ADO.NET repository, controller, models आणि quarter नसल्यास popup चे example जोडले आहे. [API setup](docs/API_SETUP.md) पहा. Visual Studio मध्ये EquityAuditDashboard.sln उघडा. Build आणि SQL execution येथे तपासलेले नाहीत.
