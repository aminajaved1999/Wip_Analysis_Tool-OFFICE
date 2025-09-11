using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.DAL
{
    public class WipRepository
    {

        #region save wip   
        public Response<string> SaveWipRecord3(WipMaster wipMaster)
        {
            var response = new Response<string>();
            try
            {
                using (var context = new WIPATContext())
                {
                    // Find existing WipMaster record by FileName and TargetMonth
                    var existingWipMaster = context.WipMasters
                        .Include(wm => wm.Details) // Include the WipDetails navigation property
                        .FirstOrDefault(wm => wm.FileName == wipMaster.FileName && wm.TargetMonth == wipMaster.TargetMonth);

                    if (existingWipMaster != null)
                    {
                        // Update the WipMaster's properties if needed
                        existingWipMaster.MOQ = wipMaster.MOQ;
                        existingWipMaster.IsCasePackChecked = wipMaster.IsCasePackChecked;
                        existingWipMaster.WipProcessingType = wipMaster.WipProcessingType;
                        existingWipMaster.Type = wipMaster.Type;

                        // Now, handle WipDetails
                        foreach (var newDetail in wipMaster.Details)
                        {
                            var existingDetail = existingWipMaster.Details.FirstOrDefault(d => d.CASIN == newDetail.CASIN && d.Month == newDetail.Month && d.Year == newDetail.Year);

                            if (existingDetail != null)
                            {
                                // Update the existing WipDetail
                                existingDetail.WipQuantity = newDetail.WipQuantity;
                                existingDetail.Stock = newDetail.Stock;
                                existingDetail.CommitmentPeriod = newDetail.CommitmentPeriod;
                                existingDetail.LaymanFormula = newDetail.LaymanFormula;
                                existingDetail.Layman = newDetail.Layman;
                                existingDetail.Analyst = newDetail.Analyst;
                                existingDetail.Review_Wip = newDetail.Review_Wip;
                                existingDetail.MOQ_Wip = newDetail.MOQ_Wip;
                                existingDetail.CasePack_Wip = newDetail.CasePack_Wip;
                                existingDetail.CasePack = newDetail.CasePack;
                                existingDetail.PODate = newDetail.PODate;
                            }
                            else
                            {
                                // Insert new WipDetail
                                context.WipDetails.Add(newDetail);
                            }
                        }

                        context.SaveChanges();
                        response.Success = true;
                        response.Message = "WIP Master and details successfully updated.";
                    }
                    else
                    {
                        // If WipMaster doesn't exist, insert it along with its details
                        context.WipMasters.Add(wipMaster);
                        context.SaveChanges();
                        response.Success = true;
                        response.Message = "WIP Master and details successfully saved.";
                    }

                    return response;
                }
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException != null ? ex.InnerException.ToString() : "No inner exception.";
                response.Success = false;
                response.Message = $"Error saving WIP Master and details: {ex.Message}. Inner Exception: {innerException}";
                return response;
            }
        }
        public async Task<bool> UpdateIsWipCalculatedAsync(string fileName)
        {
            try
            {
                using (var context = new WIPATContext())
                {
                    // Find the forecast record by FileName
                    var forecastRecord = await context.ForecastMasters.FirstOrDefaultAsync(fm => fm.FileName == fileName);

                    // If the record exists, update the IsWipCalculated column
                    if (forecastRecord != null)
                    {
                        forecastRecord.IsWipCalculated = true;
                        await context.SaveChangesAsync(); // Save the changes to the forecast record
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        #endregion save wip

    }
}
