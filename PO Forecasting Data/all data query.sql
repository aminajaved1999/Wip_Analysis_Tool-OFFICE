-- ActualOrders
SELECT * FROM ActualOrders WHERE Month = 'october' AND ItemCatalogueId IN (SELECT Id FROM ItemCatalogues );

-- InitialStocks
SELECT * FROM InitialStocks WHERE ItemCatalogueId IN (SELECT Id FROM ItemCatalogues);

--==== Forecast ================================
--select * from forecastmasters
-- August
SELECT d.* FROM ForecastDetails d INNER JOIN ForecastMasters m ON d.POForecastMasterId = m.Id WHERE m.Year = 2025 AND m.Month = 'September'

-- September
SELECT d.* FROM ForecastDetails d INNER JOIN ForecastMasters m ON d.POForecastMasterId = m.Id WHERE m.Year = 2025 AND m.Month = 'october'

--==== wip ================================
-- August
--SELECT d.* FROM WipDetails d INNER JOIN WipMasters m ON d.WipMaster_Id = m.Id WHERE m.issuedYear = 2025 AND m.issuedMonth = 'August'  
-- September
--SELECT d.* FROM WipDetails d INNER JOIN WipMasters m ON d.WipMaster_Id = m.Id WHERE m.issuedYear = 2025 AND m.issuedMonth = 'September' 