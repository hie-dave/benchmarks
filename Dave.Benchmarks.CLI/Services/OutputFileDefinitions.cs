using System.Collections.Immutable;
using Dave.Benchmarks.CLI.Models;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Core.Models.Importer;

namespace Dave.Benchmarks.CLI.Services;

/// <summary>
/// Provides metadata about known output file types
/// </summary>
public static class OutputFileDefinitions
{
    private static readonly ImmutableDictionary<string, OutputFileMetadata> Definitions;

    static OutputFileDefinitions()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, OutputFileMetadata>();

        // Daily PFT-level outputs
        AddPftOutput(builder, "file_dave_lai", "Leaf Area Index", "Daily Leaf Area Index", "m2/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_fpc", "Foliar Projective Cover", "Daily FPC", "", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_crownarea", "Crown Area", "Daily Crown Area", "m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_agd_g", "Gross Photosynthesis", "Daily gross photosynthesis", "gC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_rd_g", "Leaf Respiration", "Daily leaf respiration", "gC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_je", "PAR-limited Photosynthesis", "Daily PAR-limited photosynthesis rate", "gC/m2/h", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_vm", "RuBisCO Capacity", "RuBisCO capacity", "gC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_fphen_activity", "Phenology Activity", "Dormancy downregulation", "0-1", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_fdev_growth", "Development Factor", "Development factor for growth demand", "0-1", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_frepr_cstruct", "Reproductive Ratio", "Ratio of reproductive to aboveground structural biomass", "", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_growth_demand", "Growth Demand", "Growth Demand", "0-1", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_transpiration", "Transpiration", "Daily individual-level transpiration", "mm/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_nscal", "N Stress Scalar", "N stress scalar", "0-1", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_nscal_mean", "N Stress Scalar Mean", "N stress scalar 5 day running mean", "0-1", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_ltor", "Leaf to Root Ratio", "Daily leaf to root ratio", "", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cue", "Carbon Use Efficiency", "Carbon use efficiency", "", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_alpha_leaf", "Leaf Sink Strength", "Daily leaf sink strength", "0-1", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_alpha_root", "Root Sink Strength", "Daily root sink strength", "0-1", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_alpha_sap", "Sap Sink Strength", "Daily sap sink strength", "0-1", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_alpha_repr", "Reproductive Sink Strength", "Daily reproductive sink strength", "0-1", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass", "Vegetation Carbon Mass", "Daily PFT-level carbon mass", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass_leaf_limit", "Leaf Pool Size", "Daily optimum leaf pool size for trees", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass_root_limit", "Root Pool Size", "Daily optimum root pool size for trees", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass_sap_limit", "Sap Pool Size", "Daily optimum sap pool size for trees", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass_repr_limit", "Reproductive Pool Size", "Daily optimum reproductive pool size for trees", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass_storage_limit", "Storage Pool Size", "Daily optimum storage pool size", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cgrow_leaf", "Leaf Growth", "Daily leaf growth", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cgrow_root", "Root Growth", "Daily root growth", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cgrow_sap", "Sap Growth", "Daily sap growth", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cgrow_repr", "Reproductive Growth", "Daily reproductive growth", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_diameter_inc", "Diameter Growth", "Daily diameter growth", "m/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_height_inc", "Height Growth", "Daily height growth", "m/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_height", "Plant Height", "Plant Height (m)", "m", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_diameter", "Stem Diameter", "Stem Diameter (m)", "m", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_dturnover_leaf", "Leaf Turnover", "Daily leaf C turnover flux", "kgC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_dturnover_root", "Root Turnover", "Daily root C turnover flux", "kgC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_dturnover_sap", "Sap Turnover", "Daily sapwood C turnover flux", "kgC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_anc_frac", "ANC Fraction", "Daily anc as fraction of total daily photosynthesis", "0-1", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_anj_frac", "ANJ Fraction", "Daily anj as fraction of total daily photosynthesis", "0-1", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_anp_frac", "ANP Fraction", "Daily anp as fraction of total daily photosynthesis", "0-1", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_dnuptake", "N Uptake", "Total daily nitrogen uptake", "kgN/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cexcess", "Carbon Overflow", "Total daily carbon overflow", "kgC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_ctolitter_leaf", "Leaf C to Litter", "Total daily C sent from the leaf pool to litter", "kgC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_ntolitter_leaf", "Leaf N to Litter", "Total daily N sent from the leaf pool to litter", "kgN/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_ctolitter_root", "Root C to Litter", "Total daily C sent from the root pool to litter", "kgC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_ntolitter_root", "Root N to Litter", "Total daily N sent from the root pool to litter", "kgN/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_ctolitter_repr", "Reproductive C to Litter", "Total daily C sent from the repr pool to litter", "kgC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_ntolitter_repr", "Reproductive N to Litter", "Total daily N sent from the repr pool to litter", "kgN/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_ctolitter_crown", "Crown C to Litter", "Total daily C sent from the crown pool to litter", "kgC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_ntolitter_crown", "Crown N to Litter", "Total daily N sent from the crown pool to litter", "kgN/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_aboveground_cmass", "Above-ground C Mass", "Daily above-ground C biomass", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_belowground_cmass", "Below-ground C Mass", "Daily below-ground C biomass", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_aboveground_nmass", "Above-ground N Mass", "Daily above-ground N biomass", "kgN/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_belowground_nmass", "Below-ground N Mass", "Daily below-ground N biomass", "kgN/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_indiv_npp", "Individual NPP", "Daily NPP", "gC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_sla", "Specific Leaf Area", "Daily specific leaf area", "m2/kgC", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass_leaf", "Leaf C Mass", "Daily leaf carbon mass", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass_leaf_brown", "Brown Leaf C Mass", "Daily brown leaf carbon mass", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_nmass_leaf", "Leaf N Mass", "Daily leaf nitrogen mass", "kgN/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass_crown", "Crown C Mass", "Daily crown carbon mass", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass_repr", "Reproductive C Mass", "Daily reproductive carbon mass", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass_root", "Root C Mass", "Daily root carbon mass", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_nmass_root", "Root N Mass", "Daily root nitrogen mass", "kgN/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_nmass", "Vegetation Nitrogen Mass", "Daily PFT-level total nitrogen mass", "kgN/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass_storage", "Storage C Mass", "Daily storage carbon mass", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass_storage_max", "Max Storage C Mass", "Daily maximum storage carbon mass", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_nmass_storage", "Storage N Mass", "Daily storage nitrogen mass", "kgN/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_nmass_storage_max", "Max Storage N Mass", "Daily maximum storage nitrogen mass", "kgN/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass_sap", "Sap C Mass", "Daily Sapwood Carbon Mass", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_nmass_sap", "Sap N Mass", "Daily Sapwood Nitrogen Mass", "kgN/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass_heart", "Heart C Mass", "Daily Heartwood Carbon Mass", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_nmass_heart", "Heart N Mass", "Daily Heartwood Nitrogen Mass", "kgN/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_nmass_repr", "Reproductive N Mass", "Daily reproductive nitrogen mass", "kgN/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_ndemand", "N Demand", "Daily nitrogen demand", "kgN/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_density", "Density", "Daily individual density", "indiv/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_sapwood_area", "Sapwood Area", "Daily sapwood area", "m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_latosa", "Leaf to Sapwood Area", "Daily leaf to sapwood area ratio", "", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_fpar", "FPAR", "Daily fraction of absorbed PAR", "0-1", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_indiv_gpp", "Individual GPP", "Daily individual gross primary production", "gC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_resp_autotrophic", "Autotrophic Respiration", "Daily autotrophic respiration", "gC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_resp_maintenance", "Maintenance Respiration", "Daily maintenance respiration", "gC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_resp_growth", "Growth Respiration", "Daily growth respiration", "gC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_layerwise_fpar", "Layerwise FPAR", "Daily layerwise fraction of absorbed PAR", "0-1", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_layerwise_lai", "Layerwise LAI", "Daily layerwise leaf area index", "m2/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_wscal", "Water Stress", "Daily water stress scalar", "0-1", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass_litter_repr", "Reproductive C Litter", "Daily reproductive carbon litter", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_nmass_litter_repr", "Reproductive N Litter", "Daily reproductive nitrogen litter", "kgN/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_dresp", "Daily Respiration", "Daily total respiration", "gC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cmass_seed_ext", "External Seed C Mass", "Daily external seed carbon mass", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_subdaily_an", "Subdaily Net Photosynthesis", "Subdaily net photosynthesis", "gC/m2/h", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_subdaily_rd", "Subdaily Dark Respiration", "Subdaily dark respiration", "gC/m2/h", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_subdaily_anc", "Subdaily Net C-limited Photosynthesis", "Subdaily net C-limited photosynthesis", "gC/m2/h", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_subdaily_anj", "Subdaily Net Light-limited Photosynthesis", "Subdaily net light-limited photosynthesis", "gC/m2/h", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_subdaily_gsw", "Subdaily Stomatal Conductance", "Subdaily stomatal conductance", "mm/s", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_subdaily_ci", "Subdaily Internal CO2", "Subdaily internal CO2 concentration", "ppm", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_subdaily_vcmax", "Subdaily Vcmax", "Subdaily maximum carboxylation rate", "μmol/m2/s", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_subdaily_jmax", "Subdaily Jmax", "Subdaily maximum electron transport rate", "μmol/m2/s", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_sw", "Soil Water", "Daily soil water content", "mm", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_swmm", "Soil Water mm", "Daily soil water content in mm", "mm", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_swvol", "Soil Water Volume", "Daily soil water content as volume fraction", "0-1", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cfluxes_patch", "Patch C Fluxes", "Daily patch-level carbon fluxes", "gC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_cfluxes_pft", "PFT C Fluxes", "Daily PFT-level carbon fluxes", "gC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_met_subdaily_temp", "Subdaily Temperature", "Subdaily temperature", "°C", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_met_subdaily_par", "Subdaily PAR", "Subdaily photosynthetically active radiation", "W/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_met_subdaily_vpd", "Subdaily VPD", "Subdaily vapor pressure deficit", "kPa", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_met_subdaily_insol", "Subdaily Insolation", "Subdaily insolation", "W/m2", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_met_subdaily_precip", "Subdaily Precipitation", "Subdaily precipitation", "mm/h", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_met_subdaily_pressure", "Subdaily Pressure", "Subdaily atmospheric pressure", "kPa", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_met_subdaily_co2", "Subdaily CO2", "Subdaily atmospheric CO2 concentration", "ppm", AggregationLevel.Patch, TemporalResolution.Daily);
        AddPftOutput(builder, "file_dave_anetps_ff_max", "Max Forest Floor Net Photosynthesis", "Maximum daily forest floor net photosynthesis", "gC/m2/day", AggregationLevel.Patch, TemporalResolution.Daily);

        // Daily individual-level outputs.
        AddOutput(builder, "file_dave_indiv_cpool", "Individual Carbon Pools", "Individual-level carbon pools", "kgC/m2", ["cmass_leaf", "cmass_root", "cmass_crown", "cmass_sap", "cmass_heart", "cmass_repr", "cmass_storage"], AggregationLevel.Individual, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_indiv_npool", "Individual Nitrogen Pools", "Individual-level nitrogen pools", "kgN/m2", ["nmass_leaf", "nmass_root", "nmass_crown", "nmass_sap", "nmass_heart", "nmass_repr", "nmass_storage"], AggregationLevel.Individual, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_indiv_lai", "Individual LAI", "Cohort-level LAI", "m2/m2", ["lai"], AggregationLevel.Individual, TemporalResolution.Daily);

        // Annual patch-level outputs
        AddOutput(builder, "file_dave_patch_age", "Patch Age", "Annual patch-level age since last disturbance (years)", "years", ["age"], AggregationLevel.Patch, TemporalResolution.Annual);
        AddOutput(builder, "file_dave_arunoff", "Annual Runoff", "Annual runoff (mm)", "mm", ["runoff"], AggregationLevel.Patch, TemporalResolution.Annual);
        AddOutput(builder, "file_dave_globfirm", "Globfirm outputs", "Annual GLOBFIRM Outputs", [
            ("fireprob", "0-1")
        ], AggregationLevel.Patch, TemporalResolution.Annual);

        AddOutput(builder, "file_dave_acpool", "Annual Carbon Pools", "Annual C pools (kgC/m2)", "kgC/m2", [
            "cmass_veg",
            "cmass_litter",
            "cmass_soil",
            "total"
        ], AggregationLevel.Patch, TemporalResolution.Annual);

        AddOutput(builder, "file_dave_anpool", "Annual Nitrogen Pools", "Annual N pools (kgN/m2)", "kgN/m2", [
            "nmass_veg",
            "nmass_litter",
            "nmass_soil",
            "total"
        ], AggregationLevel.Patch, TemporalResolution.Annual);

        AddOutput(builder, "file_dave_acflux", "Annual Carbon Fluxes", "Annual C flux (gC/m2)", "gC/m2", [
            "npp",
            "gpp",
            "ra",
            "rh"
        ], AggregationLevel.Patch, TemporalResolution.Annual);

        AddMonthlyOutput(builder, "file_dave_mwcont_upper", "Monthly Upper Water Content", "Monthly wcont_upper output file (0-1)", "0-1", AggregationLevel.Patch);
        AddMonthlyOutput(builder, "file_dave_mwcont_lower", "Monthly Lower Water Content", "Monthly wcont_lower output file (0-1)", "0-1", AggregationLevel.Patch);
        AddOutput(builder, "file_dave_apet", "Annual Potential Evapotranspiration", "Annual potential patch-level evapotranspiration (mm)", "mm", ["pet"], AggregationLevel.Patch, TemporalResolution.Annual);
        AddOutput(builder, "file_dave_asimfire", "Annual Simfire Analysis", "Annual simfire analysis", [
            ("burned_area", "fraction"),
            ("fire_carbon", "gC/m2") 
        ], AggregationLevel.Patch, TemporalResolution.Annual);

        AddOutput(builder, "file_dave_afuel", "Annual Fuel Availability", "Annual blaze fuel availability (gC/m2)", "gC/m2", ["fuel"], AggregationLevel.Patch, TemporalResolution.Annual);
        AddOutput(builder, "file_dave_acoarse_woody_debris", "Annual Coarse Woody Debris", "Annual coarse woody debris (gC/m2)", "gC/m2", ["cwd"], AggregationLevel.Patch, TemporalResolution.Annual);
        AddOutput(builder, "file_dave_amet_year", "Annual Met Year", "Current year of met data being used", "year", ["year"], AggregationLevel.Patch, TemporalResolution.Annual);
        AddOutput(builder, "file_dave_aco2", "Annual CO2", "Annual atmospheric co2 concentration (ppm)", "ppm", ["co2"], AggregationLevel.Patch, TemporalResolution.Annual);
        AddOutput(builder, "file_dave_aminleach", "Annual Mineral N Leaching", "Leaching of soil mineral N (kgN/m2/yr)", "kgN/m2/yr", ["aminleach"], AggregationLevel.Patch, TemporalResolution.Annual);
        AddOutput(builder, "file_dave_sompool_acmass", "Annual SOM Pool C Mass", "Daily SOM pool C mass (kgC/m2)", "kgC/m2", [
            "SURFSTRUCT",
            "SOILSTRUCT",
            "SOILMICRO",
            "SOILACTIVE",
            "SOILSLOW",
            "SOILPASSIVE",
            "SURFMETA",
            "SURFMICRO",
            "SURFFWD",
            "SURFCWD",
            "total"
        ], AggregationLevel.Patch, TemporalResolution.Annual);

        AddOutput(builder, "file_dave_sompool_anmass", "Annual SOM Pool N Mass", "Daily SOM pool N mass (kgN/m2)", "kgN/m2", [
            "SURFSTRUCT",
            "SOILSTRUCT",
            "SOILMICRO",
            "SOILACTIVE",
            "SOILSLOW",
            "SOILPASSIVE",
            "SURFMETA",
            "SURFMICRO",
            "SURFFWD",
            "SURFCWD",
            "total"
        ], AggregationLevel.Patch, TemporalResolution.Annual);

        AddOutput(builder, "file_dave_andep", "Annual N Deposition", "Annual nitrogen deposition (kgN/m2)", "kgN/m2", [
            "dNO3dep",
            "dNH4dep",
            "nfert",
            "total"
        ], AggregationLevel.Patch, TemporalResolution.Annual);

        AddOutput(builder, "file_dave_anfixation", "Annual N Fixation", "Total annual biological N fixation (kgN/m2)", "kgN/m2", [
            "nfixation"
        ], AggregationLevel.Patch, TemporalResolution.Annual);

        // Daily patch-level outputs
        AddOutput(builder, "file_dave_daylength", "Daylength", "Daily patch-level day-length (h)", "h", ["daylength"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_soil_nmass_avail", "Available Soil N", "Available Soil N for plant uptake (kgN/m2)", "kgN/m2", ["soil_nmass_avail"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_dsimfire", "Daily Simfire Analysis", "Daily simfire analysis", [
            ("burned_area", "fraction"),
            ("fire_carbon", "gC/m2") 
        ], AggregationLevel.Patch, TemporalResolution.Daily);

        AddOutput(builder, "file_dave_met_pressure", "Met Pressure", "Daily atmospheric pressure", "kPa", ["pressure"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_met_co2", "Met CO2", "Daily atmospheric CO2 concentration", "ppm", ["co2"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_sompool_cmass", "SOM Pool C Mass", "Daily SOM pool C mass", "kgC/m2", ["cmass"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_sompool_nmass", "SOM Pool N Mass", "Daily SOM pool N mass", "kgN/m2", ["nmass"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_ninput", "N Input", "Daily nitrogen input", "kgN/m2/day", ["ninput"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_fpar_ff", "Forest Floor FPAR", "Daily forest floor FPAR", "0-1", ["fpar_ff"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_resp_heterotrophic", "Heterotrophic Respiration", "Daily heterotrophic respiration", "gC/m2/day", ["resp_h"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_resp", "Total Respiration", "Daily total respiration", "gC/m2/day", ["resp"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_gpp", "GPP", "Daily gross primary production", "gC/m2/day", ["gpp"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_npp", "NPP", "Daily net primary production", "gC/m2/day", ["npp"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_nee", "NEE", "Daily net ecosystem exchange", "gC/m2/day", ["nee"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_evaporation", "Evaporation", "Daily evaporation", "mm/day", ["evap"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_soilc", "Soil Carbon", "Daily soil carbon content", "kgC/m2", ["soilc"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_soiln", "Soil Nitrogen", "Daily soil nitrogen content", "kgN/m2", ["soiln"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_soil_nflux", "Soil N Flux", "Daily soil nitrogen flux", "kgN/m2/day", ["nflux"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_dfuel", "Daily Fuel Availability", "Daily blaze fuel availability (kgC/m2)", "kgC/m2", ["fuel"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_dcoarse_woody_debris", "Daily Coarse Woody Debris", "Daily coarse woody debris (gC/m2)", "gC/m2", ["cwd"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_met_temp", "Temperature", "Daily air temperature (°C)", "°C", ["temp"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_met_par", "PAR", "Daily PAR (J/m2/timestep)", "kJ/m2/timestep", ["par"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_met_vpd", "VPD", "Daily VPD (kPa)", "kPa", ["vpd"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_met_insol", "Insolation", "Daily insolation", "", ["insol"], AggregationLevel.Patch, TemporalResolution.Daily);
        AddOutput(builder, "file_dave_met_precip", "Precipitation", "Daily total precipitation (mm)", "mm", ["precip"], AggregationLevel.Patch, TemporalResolution.Daily);

        // Annual patch-level PFT outputs.
        AddPftOutput(builder, "file_dave_alai", "Annual LAI", "Annual LAI(m2/m2)", "m2/m2", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_afpc", "Annual FPC", "Annual FPC (0-1)", "0-1", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_acmass", "Annual C Mass", "Annual cmass (kgC/m2)", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_anmass", "Annual N Mass", "Annual nmass (kgN/m2)", "kgN/m2", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_aheight", "Annual Height", "Annual plant height (m)", "m", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_aaet", "Annual AET", "Annual actual evapotranspiration (mm)", "mm", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_adensity", "Annual Density", "Annual density of individuals over patch (/m2)", "/m2", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_altor", "Annual Leaf to Root Ratio", "Annual leaf to root ratio (unitless)", "", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_anuptake", "Annual N Uptake", "Total annual nitrogen uptake (kgN/m2/year)", "kgN/m2/year", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_a_aboveground_cmass", "Annual Aboveground C Mass", "Annual total above-ground C biomass (kgC/m2)", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_a_belowground_cmass", "Annual Belowground C Mass", "Annual total below-ground C biomass (kgC/m2)", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_a_aboveground_nmass", "Annual Aboveground N Mass", "Annual total above-ground N biomass (kgN/m2)", "kgN/m2", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_a_belowground_nmass", "Annual Belowground N Mass", "Annual total below-ground N biomass (kgN/m2)", "kgN/m2", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_anpp", "Annual NPP", "Total individual-level Annual NPP (kgC/m2/year)", "kgC/m2/year", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_agpp", "Annual GPP", "Total individual-level Annual GPP (kgC/m2/year)", "kgC/m2/year", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_aresp", "Annual Respiration", "Total individual-level Annual autotrophic Respiration (kgC/m2/year)", "kgC/m2/year", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_acmass_mort", "Annual Mortality C Mass", "Mass of annual killed vegetation (kgC/m2/year)", "kgC/m2/year", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_aclitter", "Annual C Litter", "Total individual-level Annual GPP (kgC/m2/year)", "kgC/m2/year", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_ancohort", "Annual Cohort Count", "Get the number of cohorts of this PFT currently established in this patch", "count", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_anetps_ff", "Annual Net Photosynthesis", "Annual net photosynthesis at forest floor (kgC/m2)", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_acalloc_leaf", "Annual C Allocation Leaf", "Total annual C allocation to leaf (kgC/m2)", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_acalloc_root", "Annual C Allocation Root", "Total annual C allocation to root (kgC/m2)", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_acalloc_repr", "Annual C Allocation Repr", "Total annual C allocation to repr (kgC/m2)", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_acalloc_sap", "Annual C Allocation Sap", "Total annual C allocation to sap (kgC/m2)", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dave_acalloc_crown", "Annual C Allocation Crown", "Total annual C allocation to crown (kgC/m2)", "kgC/m2", AggregationLevel.Patch, TemporalResolution.Annual);

        // Annual stand-level outputs
        AddOutput(builder, "file_dave_stand_frac", "Stand Fraction", "Fraction of the gridcell occupied by each stand", "", ["fraction"], AggregationLevel.Stand, TemporalResolution.Annual);
        AddOutput(builder, "file_dave_stand_type", "Stand Type", "Stand landcover types", "", ["type"], AggregationLevel.Stand, TemporalResolution.Annual);

        // Annual gridcell-level PFT outputs.
        AddPftOutput(builder, "file_cmass", "C Mass", "Total carbon biomass (kgC/m2)", "kgC/m2", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddPftOutput(builder, "file_anpp", "Annual NPP", "Annual Net Primary Production (kgC/m2/year)", "kgC/m2/year", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddPftOutput(builder, "file_agpp", "Annual GPP", "Annual Gross Primary Production (kgC/m2/year)", "kgC/m2/year", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddPftOutput(builder, "file_fpc", "Foliage Projective Cover", "Foliage Projective Cover (fraction)", "0-1", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddPftOutput(builder, "file_aaet", "Annual AET", "Annual Actual Evapotranspiration (mm/year)", "mm/year", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddPftOutput(builder, "file_lai", "Leaf Area Index", "Leaf Area Index (m2/m2)", "m2/m2", AggregationLevel.Gridcell, TemporalResolution.Annual);

        // Annual gridcell-level outputs.
        AddOutput(builder, "file_cflux", "Carbon Fluxes", "Annual carbon fluxes (kgC/m2/year)", "kgC/m2/year", new[] {
            "Veg",
            "Repr",
            "Soil",
            "Fire",
            "Est",
            "Seed",
            "Harvest",
            "LU_ch",
            "Slow_h",
            "NEE"
        }, AggregationLevel.Gridcell, TemporalResolution.Annual);

        AddPftOutput(builder, "file_doc", "Dissolved Organic Carbon", "Dissolved organic carbon (kgC/m2)", "kgC/m2", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddPftOutput(builder, "file_dens", "Tree Density", "Tree density (indiv/m2)", "indiv/m2", AggregationLevel.Gridcell, TemporalResolution.Annual);

        AddOutput(builder, "file_cpool", "Carbon Pools", "Soil carbon pools (kgC/m2)", "kgC/m2", [
            "VegC",
            "LitterC",
            "SoilC",
            "Total"
        ], AggregationLevel.Gridcell, TemporalResolution.Annual);

        AddPftOutput(builder, "file_clitter", "Carbon Litter", "Carbon in litter (kgC/m2)", "kgC/m2", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddOutput(builder, "file_runoff", "Runoff", "Total runoff (mm/year)", "mm/year", [
            "Surf",
            "Drain",
            "Base",
            "Total"], AggregationLevel.Gridcell, TemporalResolution.Annual);

        AddOutput(builder, "file_wetland_water_added", "Wetland Water Added", "Water added to wetland (mm)", "mm", ["H2OAdded"], AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddPftOutput(builder, "file_speciesheights", "Species Height", "Mean Species Height", "m", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddPftOutput(builder, "file_speciesdiam", "Species Diameter", "Mean species diameter", "m", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddOutput(builder, "file_firert", "Fire Return Time", "Fire return time", [
            ("FireRT", "years"),
            ("BurntFr", "0-1")], AggregationLevel.Gridcell, TemporalResolution.Annual);

        AddPftOutput(builder, "file_nmass", "N Mass", "Total nitrogen in biomass (kgN/m2)", "kgN/m2", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddPftOutput(builder, "file_cton_leaf", "Leaf C:N Ratio", "Carbon to Nitrogen ratio in leaves", "", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddOutput(builder, "file_nsources", "N Sources", "Annual nitrogen sources (kgN/m2/year)", "kgN/m2/year", [
            "NH4dep",
            "NO3dep",
            "fix",
            "fert",
            "input",
            "min",
            "imm",
            "netmin",
            "Total"
        ], AggregationLevel.Gridcell, TemporalResolution.Annual);

        AddOutput(builder, "file_npool", "N Pools", "Soil nitrogen pools (kgN/m2)", "kgN/m2", [
            "VegN",
            "LitterN",
            "SoilN",
            "Total"
        ], AggregationLevel.Gridcell, TemporalResolution.Annual);

        AddPftOutput(builder, "file_nlitter", "N Litter", "Nitrogen in litter (kgN/m2)", "kgN/m2", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddPftOutput(builder, "file_nuptake", "N Uptake", "Annual nitrogen uptake (kgN/m2/year)", "kgN/m2/year", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddPftOutput(builder, "file_vmaxnlim", "Vmax N Limitation", "Annual nitrogen limitation on Vmax", "", AggregationLevel.Gridcell, TemporalResolution.Annual);

        AddOutput(builder, "file_nflux", "N Fluxes", "Annual nitrogen fluxes (kgN/m2/year)", "kgN/m2/year", [
            "NH4dep",
            "NO3dep",
            "fix",
            "fert",
            "est",
            "flux",
            "leach",
            "NEE",
            "Total"
        ], AggregationLevel.Gridcell, TemporalResolution.Annual);

        AddOutput(builder, "file_ngases", "N Gas Emissions", "Annual nitrogen gas emissions (kgN/m2/year)", "kgN/m2/year", [
            "NH3_fire",
            "NH3_soil",
            "NOx_fire",
            "NOx_soil",
            "N2O_fire",
            "N2O_soil",
            "N2_fire",
            "N2_soil",
            "Total"
        ], AggregationLevel.Gridcell, TemporalResolution.Annual);

        AddPftOutput(builder, "file_aiso", "Isoprene Flux", "Annual Isoprene Flux (mgC/m2/year)", "mgC/m2/year", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddPftOutput(builder, "file_amon", "Monoterpene Flux", "Annual Monoterpene Flux (mgC/m2/year)", "mgC/m2/year", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddPftOutput(builder, "file_amon_mt1", "Endocyclic Monoterpene Flux", "Annual Endocyclic Monoterpene Flux (mgC/m2/year)", "mgC/m2/year", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddPftOutput(builder, "file_amon_mt2", "Other Monoterpene Flux", "Annual Other Monoterpene Flux (mgC/m2/year)", "mgC/m2/year", AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddOutput(builder, "file_aburned_area_out", "BLAZE Burned Area", "Annual BLAZE Burned Area", [("BurntFr", "0-1")], AggregationLevel.Gridcell, TemporalResolution.Annual);
        AddOutput(builder, "file_simfireanalysis_out", "SIMFIRE Analytics", "Annual SIMFIRE Analytics", [
            ("Biome", @"0=NOVEG, 1=CROP, 2=NEEDLELEAF, 3=BROADLEAF, 4=MIXED_FOREST, 5=SHRUBS, 6=SAVANNA, 7=TUNDRA, 8=BARREN"),
            ("MxNest", "-"),
            ("PopDens", "inhabitants/km2"),
            ("AMxFApar", "0-1"),
            ("FireProb", "0-1"),
            ("Region", "unused")], AggregationLevel.Gridcell, TemporalResolution.Annual);

        // Monthly gridcell-level outputs
        AddMonthlyOutput(builder, "file_mnpp", "Monthly NPP", "Monthly Net Primary Production (kgC/m2/month)", "kgC/m2/month", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mlai", "Monthly LAI", "Monthly Leaf Area Index (m2/m2)", "m2/m2", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mgpp", "Monthly GPP", "Monthly Gross Primary Production (kgC/m2/month)", "kgC/m2/month", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mra", "Monthly Ra", "Monthly autotrophic respiration (kgC/m2/month)", "kgC/m2/month", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_maet", "Monthly AET", "Monthly Actual Evapotranspiration (mm/month)", "mm/month", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mpet", "Monthly PET", "Monthly Potential Evapotranspiration (mm/month)", "mm/month", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mevap", "Monthly Evap", "Monthly Evaporation (mm/month)", "mm/month", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mrunoff", "Monthly Runoff", "Monthly runoff (mm/month)", "mm/month", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mintercep", "Monthly Interception", "Monthly interception (mm/month)", "mm/month", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mrh", "Monthly Rh", "Monthly heterotrophic respiration (kgC/m2/month)", "kgC/m2/month", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mnee", "Monthly NEE", "Monthly Net Ecosystem Exchange (kgC/m2/month)", "kgC/m2/month", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mwcont_upper", "Monthly Upper Water Content", "Monthly upper soil water content (fraction)", "", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mwcont_lower", "Monthly Lower Water Content", "Monthly lower soil water content (fraction)", "", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_miso", "Monthly Isoprene", "Monthly isoprene flux (mgC/m2/month)", "mgC/m2/month", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mmon", "Monthly Monoterpene", "Monthly monoterpene flux (mgC/m2/month)", "mgC/m2/month", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mmon_mt1", "Endocyclic Monoterpene Flux", "Monthly Endocyclic Monoterpene Flux", "mgC/m2", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mmon_mt2", "Other Monoterpene Flux", "Monthly Endocyclic Monoterpene Flux", "mgC/m2", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_msoiltempdepth5", "Soil temperature (5cm)", "Soil temperature (5cm)", "degC", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_msoiltempdepth15", "Soil temperature (15cm)", "Soil temperature (15cm)", "degC", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_msoiltempdepth25", "Soil temperature (25cm)", "Soil temperature (25cm)", "degC", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_msoiltempdepth35", "Soil temperature (35cm)", "Soil temperature (35cm)", "degC", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_msoiltempdepth45", "Soil temperature (45cm)", "Soil temperature (45cm)", "degC", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_msoiltempdepth55", "Soil temperature (55cm)", "Soil temperature (55cm)", "degC", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_msoiltempdepth65", "Soil temperature (65cm)", "Soil temperature (65cm)", "degC", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_msoiltempdepth75", "Soil temperature (75cm)", "Soil temperature (75cm)", "degC", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_msoiltempdepth85", "Soil temperature (85cm)", "Soil temperature (85cm)", "degC", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_msoiltempdepth95", "Soil temperature (95cm)", "Soil temperature (95cm)", "degC", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_msoiltempdepth105", "Soil temperature (105cm)", "Soil temperature (105cm)", "degC", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_msoiltempdepth115", "Soil temperature (115cm)", "Soil temperature (115cm)", "degC", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_msoiltempdepth125", "Soil temperature (125cm)", "Soil temperature (125cm)", "degC", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_msoiltempdepth135", "Soil temperature (135cm)", "Soil temperature (135cm)", "degC", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_msoiltempdepth145", "Soil temperature (145cm)", "Soil temperature (145cm)", "degC", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mch4", "Monthly CH4 emissions, total", "Monthly CH4 emissions, total", "kg C/m2/yr", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mch4diff", "Monthly CH4 emissions, diffusion", "Monthly CH4 emissions, diffusion", "kg C/m2/yr", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mch4plan", "Monthly CH4 emissions, plant-mediated", "Monthly CH4 emissions, plant-mediated", "kg C/m2/yr", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mch4ebull", "Monthly CH4 emissions, ebullition", "Monthly CH4 emissions, ebullition", "kg C/m2/yr", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_msnow", "Monthly snow depth", "Monthly snow depth", "m", AggregationLevel.Gridcell);
        AddMonthlyOutput(builder, "file_mwtp", "Monthly water table depth", "Monthly water table depth", "m", AggregationLevel.Gridcell);
        AddOutput(builder, "file_mald", "Monthly active layer depth", "Monthly active layer depth",
            ModelConstants.MonthCols.Select(c => (c, "m")).Concat(
                [("MAXALD", "m")]
            ).ToArray(), AggregationLevel.Gridcell, TemporalResolution.Monthly);
        AddMonthlyOutput(builder, "file_mburned_area_out", "BLAZE Monthly Burned Area", "BLAZE Monthly Burned Area", "0-1", AggregationLevel.Gridcell);

        Definitions = builder.ToImmutable();
    }

    /// <summary>
    /// Get metadata for a specific output file type
    /// </summary>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out).</param>
    /// <returns>Metadata about the output file structure, or null if not a known type</returns>
    /// <exception cref="InvalidOperationException">Thrown if no metadata is found for the specified type.</exception>
    public static OutputFileMetadata GetMetadata(string fileType)
    {
        if (Definitions.TryGetValue(fileType, out var quantity))
            return quantity;
        throw new InvalidOperationException($"Unable to find metadata for unknown output file type: {fileType}");
    }

    /// <summary>
    /// Get all known output file types.
    /// </summary>
    public static IEnumerable<string> GetAllFileTypes() => Definitions.Keys;

    /// <summary>
    /// Register metadata for an output file.
    /// </summary>
    /// <param name="builder">The dictionary builder.</param>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out).</param>
    /// <param name="name">The name of the output file (e.g., "Leaf Area Index").</param>
    /// <param name="description">A description of the output file (e.g., "Annual Leaf Area Index").</param>
    /// <param name="layers">A list of pairs of (layer name, units).</param>
    /// <param name="level">The level at which data is aggregated.</param>
    /// <param name="resolution">The temporal resolution of the data.</param>
    private static void AddOutput(ImmutableDictionary<string, OutputFileMetadata>.Builder builder, string fileType, string name, string description, (string layer, string units)[] layers, AggregationLevel level, TemporalResolution resolution)
    {
        builder.Add(fileType, new OutputFileMetadata(
            fileName: fileType,
            name: name,
            description: description,
            layers: new StaticLayers(layers.Select(l => (l.layer, new Unit(l.units))).ToArray(), level, resolution),
            level: level,
            resolution: resolution));
    }

    /// <summary>
    /// Register metadata for an output file.
    /// </summary>
    /// <param name="builder">The dictionary builder.</param>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out).</param>
    /// <param name="name">The name of the output file (e.g., "Leaf Area Index").</param>
    /// <param name="description">A description of the output file (e.g., "Annual Leaf Area Index").</param>
    /// <param name="units">The units of all columns in the output file.</param>
    /// <param name="layers">A list of layer names.</param>
    /// <param name="level">The level at which data is aggregated.</param>
    /// <param name="resolution">The temporal resolution of the data.</param>
    private static void AddOutput(ImmutableDictionary<string, OutputFileMetadata>.Builder builder, string fileType, string name, string description, string units, string[] layers, AggregationLevel level, TemporalResolution resolution)
    {
        builder.Add(fileType, new OutputFileMetadata(
            fileName: fileType,
            name: name,
            description: description,
            layers: new StaticLayers(layers.Select(l => (l, new Unit(units))).ToArray(), level, resolution),
            level: level,
            resolution: resolution));
    }

    /// <summary>
    /// Register metadata for an output file.
    /// </summary>
    /// <param name="builder">The dictionary builder.</param>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out).</param>
    /// <param name="name">The name of the output file (e.g., "Leaf Area Index").</param>
    /// <param name="description">A description of the output file (e.g., "Annual Leaf Area Index").</param>
    /// <param name="units">The units of all columns in the output file (e.g., "m2/m2").</param>
    /// <param name="level">The level at which data is aggregated.</param>
    /// <param name="resolution">The temporal resolution of the data.</param>
    /// <remarks>
    /// Note: Monthly resolution is not supported for dynamic layers.
    /// </remarks>
    private static void AddPftOutput(ImmutableDictionary<string, OutputFileMetadata>.Builder builder, string fileType, string name, string description, string units, AggregationLevel level, TemporalResolution resolution)
    {
        builder.Add(fileType, new OutputFileMetadata(
            fileName: fileType,
            name: name,
            description: description,
            layers: new DynamicLayers(new Unit(units), level, resolution),
            level: level,
            resolution: resolution));
    }

    /// <summary>
    /// Register metadata for a monthly output file.
    /// </summary>
    /// <param name="builder">The dictionary builder.</param>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out).</param>
    /// <param name="name">The name of the output file (e.g., "Leaf Area Index").</param>
    /// <param name="description">A description of the output file (e.g., "Annual Leaf Area Index").</param>
    /// <param name="units">The units of all columns in the output file (e.g., "m2/m2").</param>
    /// <param name="level">The level at which data is aggregated.</param>
    private static void AddMonthlyOutput(ImmutableDictionary<string, OutputFileMetadata>.Builder builder, string fileType, string name, string description, string units, AggregationLevel level)
    {
        builder.Add(fileType, new OutputFileMetadata(
            fileName: fileType,
            name: name,
            description: description,
            layers: new StaticLayers(ModelConstants.MonthCols.Select(c => (c, new Unit(units))).ToArray(), level, TemporalResolution.Monthly),
            level: level,
            resolution: TemporalResolution.Monthly));
    }
}
