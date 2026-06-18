SELECT
    -- ItemCatalogues
    (SELECT COUNT(casin) FROM ItemCatalogues) AS ItemCatalogues_TotalCount,
    (SELECT COUNT(*) FROM ItemCatalogues WHERE isActive = 0) AS ItemCatalogues_InactiveCount,
    (SELECT COUNT(*) FROM ItemCatalogues WHERE isActive = 1) AS ItemCatalogues_ActiveCount;

-- ForecastDetails in April 2026
SELECT
    COUNT(DISTINCT fd.casin) AS ForecastDetails_TotalCount_inApril2026,
    COUNT(DISTINCT CASE WHEN fd.isActive = 0 THEN fd.casin END) AS ForecastDetails_InactiveCount_inApril2026,
    COUNT(DISTINCT CASE WHEN fd.isActive = 1 THEN fd.casin END) AS ForecastDetails_ActiveCount_inApril2026
FROM forecastdetails fd INNER JOIN ForecastMasters fm ON fm.id = fd.POForecastMasterId WHERE fm.month = 'April' AND fm.year = 2026;

   -- ForecastDetails in May 2026
SELECT
    COUNT(DISTINCT fd.casin) AS ForecastDetails_TotalCount_inMay2026,
    COUNT(DISTINCT CASE WHEN fd.isActive = 0 THEN fd.casin END) AS ForecastDetails_InactiveCount_inMay2026,
    COUNT(DISTINCT CASE WHEN fd.isActive = 1 THEN fd.casin END) AS ForecastDetails_ActiveCount_inMay2026
FROM forecastdetails fd INNER JOIN ForecastMasters fm ON fm.id = fd.POForecastMasterId WHERE fm.month = 'May' AND fm.year = 2026;


select * from ItemCatalogues where isactive = 0;


update ForecastMasters set  IsWipCalculated = 0 where month = 'May' AND year = 2026;


delete from ForecastDetails where POForecastMasterId in (select Id from ForecastMasters where month = 'may' and year = '2026');
delete from ForecastMasters where month = 'may' and year = '2026';


delete from WipDetails where WipMaster_Id in (select id from WipMasters where IssuedMonth ='may' and IssuedYear = '2026');
delete from WipMasters where IssuedMonth ='may' and IssuedYear = '2026';

select * from ItemCatalogues

select * from WipDetails
order by id desc