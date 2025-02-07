using System.Collections.Immutable;
using Dave.Benchmarks.CLI.Models;

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
        AddPftOutput(builder, "dave_lai", "Leaf Area Index", "Daily Leaf Area Index", "m2/m2");
        AddPftOutput(builder, "dave_fpc", "Foliar Projective Cover", "Daily FPC", "");
        AddPftOutput(builder, "dave_crownarea", "Crown Area", "Daily Crown Area", "m2");
        AddPftOutput(builder, "dave_agd_g", "Gross Photosynthesis", "Daily gross photosynthesis", "gC/m2/day");
        AddPftOutput(builder, "dave_rd_g", "Leaf Respiration", "Daily leaf respiration", "gC/m2/day");
        AddPftOutput(builder, "dave_je", "PAR-limited Photosynthesis", "Daily PAR-limited photosynthesis rate", "gC/m2/h");
        AddPftOutput(builder, "dave_vm", "RuBisCO Capacity", "RuBisCO capacity", "gC/m2/day");
        AddPftOutput(builder, "dave_fphen_activity", "Phenology Activity", "Dormancy downregulation", "0-1");
        AddPftOutput(builder, "dave_fdev_growth", "Development Factor", "Development factor for growth demand", "0-1");
        AddPftOutput(builder, "dave_frepr_cstruct", "Reproductive Ratio", "Ratio of reproductive to aboveground structural biomass", "");
        AddPftOutput(builder, "dave_growth_demand", "Growth Demand", "Growth Demand", "0-1");
        AddPftOutput(builder, "dave_transpiration", "Transpiration", "Daily individual-level transpiration", "mm/day");
        AddPftOutput(builder, "dave_nscal", "N Stress Scalar", "N stress scalar", "0-1");
        AddPftOutput(builder, "dave_nscal_mean", "N Stress Scalar Mean", "N stress scalar 5 day running mean", "0-1");
        AddPftOutput(builder, "dave_ltor", "Leaf to Root Ratio", "Daily leaf to root ratio", "");
        AddPftOutput(builder, "dave_cue", "Carbon Use Efficiency", "Carbon use efficiency", "");
        AddPftOutput(builder, "dave_alpha_leaf", "Leaf Sink Strength", "Daily leaf sink strength", "0-1");
        AddPftOutput(builder, "dave_alpha_root", "Root Sink Strength", "Daily root sink strength", "0-1");
        AddPftOutput(builder, "dave_alpha_sap", "Sap Sink Strength", "Daily sap sink strength", "0-1");
        AddPftOutput(builder, "dave_alpha_repr", "Reproductive Sink Strength", "Daily reproductive sink strength", "0-1");
        AddPftOutput(builder, "dave_cmass", "Vegetation Carbon Mass", "Daily PFT-level carbon mass", "kgC/m2");
        AddPftOutput(builder, "dave_cmass_leaf_limit", "Leaf Pool Size", "Daily optimum leaf pool size for trees", "kgC/m2");
        AddPftOutput(builder, "dave_cmass_root_limit", "Root Pool Size", "Daily optimum root pool size for trees", "kgC/m2");
        AddPftOutput(builder, "dave_cmass_sap_limit", "Sap Pool Size", "Daily optimum sap pool size for trees", "kgC/m2");
        AddPftOutput(builder, "dave_cmass_repr_limit", "Reproductive Pool Size", "Daily optimum reproductive pool size for trees", "kgC/m2");
        AddPftOutput(builder, "dave_cmass_storage_limit", "Storage Pool Size", "Daily optimum storage pool size", "kgC/m2");
        AddPftOutput(builder, "dave_cgrow_leaf", "Leaf Growth", "Daily leaf growth", "kgC/m2");
        AddPftOutput(builder, "dave_cgrow_root", "Root Growth", "Daily root growth", "kgC/m2");
        AddPftOutput(builder, "dave_cgrow_sap", "Sap Growth", "Daily sap growth", "kgC/m2");
        AddPftOutput(builder, "dave_cgrow_repr", "Reproductive Growth", "Daily reproductive growth", "kgC/m2");
        AddPftOutput(builder, "dave_diameter_inc", "Diameter Growth", "Daily diameter growth", "m/day");
        AddPftOutput(builder, "dave_height_inc", "Height Growth", "Daily height growth", "m/day");
        AddPftOutput(builder, "dave_dturnover_leaf", "Leaf Turnover", "Daily leaf C turnover flux", "kgC/m2/day");
        AddPftOutput(builder, "dave_dturnover_root", "Root Turnover", "Daily root C turnover flux", "kgC/m2/day");
        AddPftOutput(builder, "dave_dturnover_sap", "Sap Turnover", "Daily sapwood C turnover flux", "kgC/m2/day");
        AddPftOutput(builder, "dave_anc_frac", "ANC Fraction", "Daily anc as fraction of total daily photosynthesis", "0-1");
        AddPftOutput(builder, "dave_anj_frac", "ANJ Fraction", "Daily anj as fraction of total daily photosynthesis", "0-1");
        AddPftOutput(builder, "dave_anp_frac", "ANP Fraction", "Daily anp as fraction of total daily photosynthesis", "0-1");
        AddPftOutput(builder, "dave_dnuptake", "N Uptake", "Total daily nitrogen uptake", "kgN/m2/day");
        AddPftOutput(builder, "dave_cexcess", "Carbon Overflow", "Total daily carbon overflow", "kgC/m2/day");
        AddPftOutput(builder, "dave_ctolitter_leaf", "Leaf C to Litter", "Total daily C sent from the leaf pool to litter", "kgC/m2/day");
        AddPftOutput(builder, "dave_ntolitter_leaf", "Leaf N to Litter", "Total daily N sent from the leaf pool to litter", "kgN/m2/day");
        AddPftOutput(builder, "dave_ctolitter_root", "Root C to Litter", "Total daily C sent from the root pool to litter", "kgC/m2/day");
        AddPftOutput(builder, "dave_ntolitter_root", "Root N to Litter", "Total daily N sent from the root pool to litter", "kgN/m2/day");
        AddPftOutput(builder, "dave_ctolitter_repr", "Reproductive C to Litter", "Total daily C sent from the repr pool to litter", "kgC/m2/day");
        AddPftOutput(builder, "dave_ntolitter_repr", "Reproductive N to Litter", "Total daily N sent from the repr pool to litter", "kgN/m2/day");
        AddPftOutput(builder, "dave_ctolitter_crown", "Crown C to Litter", "Total daily C sent from the crown pool to litter", "kgC/m2/day");
        AddPftOutput(builder, "dave_ntolitter_crown", "Crown N to Litter", "Total daily N sent from the crown pool to litter", "kgN/m2/day");
        AddPftOutput(builder, "dave_aboveground_cmass", "Above-ground C Mass", "Daily above-ground C biomass", "kgC/m2");
        AddPftOutput(builder, "dave_belowground_cmass", "Below-ground C Mass", "Daily below-ground C biomass", "kgC/m2");
        AddPftOutput(builder, "dave_aboveground_nmass", "Above-ground N Mass", "Daily above-ground N biomass", "kgN/m2");
        AddPftOutput(builder, "dave_belowground_nmass", "Below-ground N Mass", "Daily below-ground N biomass", "kgN/m2");
        AddPftOutput(builder, "dave_indiv_npp", "Individual NPP", "Daily NPP", "gC/m2/day");
        AddPftOutput(builder, "dave_sla", "Specific Leaf Area", "Daily specific leaf area", "m2/kgC");
        AddPftOutput(builder, "dave_cmass_leaf_brown", "Brown Leaf C Mass", "Daily brown leaf carbon mass", "kgC/m2");
        AddPftOutput(builder, "dave_nmass_leaf", "Leaf N Mass", "Daily leaf nitrogen mass", "kgN/m2");
        AddPftOutput(builder, "dave_cmass_crown", "Crown C Mass", "Daily crown carbon mass", "kgC/m2");
        AddPftOutput(builder, "dave_nmass_root", "Root N Mass", "Daily root nitrogen mass", "kgN/m2");
        AddPftOutput(builder, "dave_nmass", "Vegetation Nitrogen Mass", "Daily PFT-level total nitrogen mass", "kgN/m2");
        AddPftOutput(builder, "dave_cmass_storage_max", "Max Storage C Mass", "Daily maximum storage carbon mass", "kgC/m2");
        AddPftOutput(builder, "dave_nmass_storage", "Storage N Mass", "Daily storage nitrogen mass", "kgN/m2");
        AddPftOutput(builder, "dave_nmass_storage_max", "Max Storage N Mass", "Daily maximum storage nitrogen mass", "kgN/m2");
        AddPftOutput(builder, "dave_nmass_sap", "Sap N Mass", "Daily sapwood nitrogen mass", "kgN/m2");
        AddPftOutput(builder, "dave_cmass_heart", "Heart C Mass", "Daily heartwood carbon mass", "kgC/m2");
        AddPftOutput(builder, "dave_nmass_heart", "Heart N Mass", "Daily heartwood nitrogen mass", "kgN/m2");
        AddPftOutput(builder, "dave_nmass_repr", "Reproductive N Mass", "Daily reproductive nitrogen mass", "kgN/m2");
        AddPftOutput(builder, "dave_ndemand", "N Demand", "Daily nitrogen demand", "kgN/m2/day");
        AddPftOutput(builder, "dave_density", "Density", "Daily individual density", "indiv/m2");
        AddPftOutput(builder, "dave_sapwood_area", "Sapwood Area", "Daily sapwood area", "m2");
        AddPftOutput(builder, "dave_latosa", "Leaf to Sapwood Area", "Daily leaf to sapwood area ratio", "");
        AddPftOutput(builder, "dave_fpar", "FPAR", "Daily fraction of absorbed PAR", "0-1");
        AddPftOutput(builder, "dave_indiv_gpp", "Individual GPP", "Daily individual gross primary production", "gC/m2/day");
        AddPftOutput(builder, "dave_resp_autotrophic", "Autotrophic Respiration", "Daily autotrophic respiration", "gC/m2/day");
        AddPftOutput(builder, "dave_resp_maintenance", "Maintenance Respiration", "Daily maintenance respiration", "gC/m2/day");
        AddPftOutput(builder, "dave_resp_growth", "Growth Respiration", "Daily growth respiration", "gC/m2/day");
        AddPftOutput(builder, "dave_layerwise_fpar", "Layerwise FPAR", "Daily layerwise fraction of absorbed PAR", "0-1");
        AddPftOutput(builder, "dave_layerwise_lai", "Layerwise LAI", "Daily layerwise leaf area index", "m2/m2");
        AddPftOutput(builder, "dave_wscal", "Water Stress", "Daily water stress scalar", "0-1");
        AddPftOutput(builder, "dave_cmass_litter_repr", "Reproductive C Litter", "Daily reproductive carbon litter", "kgC/m2");
        AddPftOutput(builder, "dave_nmass_litter_repr", "Reproductive N Litter", "Daily reproductive nitrogen litter", "kgN/m2");
        AddPftOutput(builder, "dave_dresp", "Daily Respiration", "Daily total respiration", "gC/m2/day");
        AddPftOutput(builder, "dave_cmass_seed_ext", "External Seed C Mass", "Daily external seed carbon mass", "kgC/m2");
        AddPftOutput(builder, "dave_subdaily_an", "Subdaily Net Photosynthesis", "Subdaily net photosynthesis", "gC/m2/h");
        AddPftOutput(builder, "dave_subdaily_rd", "Subdaily Dark Respiration", "Subdaily dark respiration", "gC/m2/h");
        AddPftOutput(builder, "dave_subdaily_anc", "Subdaily Net C-limited Photosynthesis", "Subdaily net C-limited photosynthesis", "gC/m2/h");
        AddPftOutput(builder, "dave_subdaily_anj", "Subdaily Net Light-limited Photosynthesis", "Subdaily net light-limited photosynthesis", "gC/m2/h");
        AddPftOutput(builder, "dave_subdaily_gsw", "Subdaily Stomatal Conductance", "Subdaily stomatal conductance", "mm/s");
        AddPftOutput(builder, "dave_subdaily_ci", "Subdaily Internal CO2", "Subdaily internal CO2 concentration", "ppm");
        AddPftOutput(builder, "dave_subdaily_vcmax", "Subdaily Vcmax", "Subdaily maximum carboxylation rate", "μmol/m2/s");
        AddPftOutput(builder, "dave_subdaily_jmax", "Subdaily Jmax", "Subdaily maximum electron transport rate", "μmol/m2/s");
        AddPftOutput(builder, "dave_sw", "Soil Water", "Daily soil water content", "mm");
        AddPftOutput(builder, "dave_swmm", "Soil Water mm", "Daily soil water content in mm", "mm");
        AddPftOutput(builder, "dave_swvol", "Soil Water Volume", "Daily soil water content as volume fraction", "0-1");
        AddPftOutput(builder, "dave_cfluxes_patch", "Patch C Fluxes", "Daily patch-level carbon fluxes", "gC/m2/day");
        AddPftOutput(builder, "dave_cfluxes_pft", "PFT C Fluxes", "Daily PFT-level carbon fluxes", "gC/m2/day");
        AddPftOutput(builder, "dave_met_subdaily_temp", "Subdaily Temperature", "Subdaily temperature", "°C");
        AddPftOutput(builder, "dave_met_subdaily_par", "Subdaily PAR", "Subdaily photosynthetically active radiation", "W/m2");
        AddPftOutput(builder, "dave_met_subdaily_vpd", "Subdaily VPD", "Subdaily vapor pressure deficit", "kPa");
        AddPftOutput(builder, "dave_met_subdaily_insol", "Subdaily Insolation", "Subdaily insolation", "W/m2");
        AddPftOutput(builder, "dave_met_subdaily_precip", "Subdaily Precipitation", "Subdaily precipitation", "mm/h");
        AddPftOutput(builder, "dave_met_subdaily_pressure", "Subdaily Pressure", "Subdaily atmospheric pressure", "kPa");
        AddPftOutput(builder, "dave_met_subdaily_co", "Subdaily CO2", "Subdaily atmospheric CO2 concentration", "ppm");
        AddPftOutput(builder, "dave_anetps_ff_max", "Max Forest Floor Net Photosynthesis", "Maximum daily forest floor net photosynthesis", "gC/m2/day");

        // Daily individual-level outputs.
        AddIndivOutput(builder, "dave_indiv_cpool", "Individual Carbon Pools", "Individual-level carbon pools", "kgC/m2", ["cmass_leaf", "cmass_root", "cmass_crown", "cmass_sap", "cmass_heart", "cmass_repr", "cmass_storage"]);
        AddIndivOutput(builder, "dave_indiv_npool", "Individual Nitrogen Pools", "Individual-level nitrogen pools", "kgN/m2", ["nmass_leaf", "nmass_root", "nmass_crown", "nmass_sap", "nmass_heart", "nmass_repr", "nmass_storage"]);
        AddIndivOutput(builder, "dave_indiv_lai", "Individual LAI", "Cohort-level LAI", "m2/m2", ["lai"]);

        // Patch-level outputs
        AddPatchOutput(builder, "dave_patch_age", "Patch Age", "Annual patch-level age since last disturbance (years)", "years", ["age"]);
        AddPatchOutput(builder, "dave_arunoff", "Annual Runoff", "Annual runoff (mm)", "mm", ["runoff"]);
        AddPatchOutput(builder, "dave_globfirm", "Globfirm outputs", "Annual GLOBFIRM Outputs", [
            ("fireprob", "0-1")
        ]);

        // Carbon pools
        AddPatchOutput(builder, "dave_acpool", "Annual Carbon Pools", "Annual C pools (kgC/m2)", "kgC/m2", [
            "cmass_veg",
            "cmass_litter",
            "cmass_soil",
            "total"
        ]);

        // Nitrogen pools
        AddPatchOutput(builder, "dave_anpool", "Annual Nitrogen Pools", "Annual N pools (kgN/m2)", "kgN/m2", [
            "nmass_veg",
            "nmass_litter",
            "nmass_soil",
            "total"
        ]);

        // Carbon fluxes 
        AddPatchOutput(builder, "dave_acflux", "Annual Carbon Fluxes", "Annual C flux (gC/m2)", "gC/m2", [
            "npp",
            "gpp",
            "ra",
            "rh"
        ]);

        // Water content
        AddPatchOutput(builder, "dave_mwcont_upper", "Monthly Upper Water Content", "Monthly wcont_upper output file (0-1)", "0-1", ["wcont_upper"]);
        AddPatchOutput(builder, "dave_mwcont_lower", "Monthly Lower Water Content", "Monthly wcont_lower output file (0-1)", "0-1", ["wcont_lower"]);

        // Evapotranspiration
        AddPatchOutput(builder, "dave_apet", "Annual Potential Evapotranspiration", "Annual potential patch-level evapotranspiration (mm)", "mm", ["pet"]);

        // Fire-related outputs
        AddPatchOutput(builder, "dave_asimfire", "Annual Simfire Analysis", "Annual simfire analysis", [
            ("burned_area", "fraction"),
            ("fire_carbon", "gC/m2") 
        ]);

        AddPatchOutput(builder, "dave_afuel", "Annual Fuel Availability", "Annual blaze fuel availability (gC/m2)", "gC/m2", ["fuel"]);

        // Woody debris
        AddPatchOutput(builder, "dave_acoarse_woody_debris", "Annual Coarse Woody Debris", "Annual coarse woody debris (gC/m2)", "gC/m2", ["cwd"]);

        // Meteorological data
        AddPatchOutput(builder, "dave_amet_year", "Annual Met Year", "Current year of met data being used", "year", ["year"]);
        AddPatchOutput(builder, "dave_aco2", "Annual CO2", "Annual atmospheric co2 concentration (ppm)", "ppm", ["co2"]);

        // Soil pools and fluxes
        AddPatchOutput(builder, "dave_aminleach", "Annual Mineral N Leaching", "Leaching of soil mineral N (kgN/m2/yr)", "kgN/m2/yr", ["aminleach"]);

        AddPatchOutput(builder, "dave_sompool_acmass", "Annual SOM Pool C Mass", "Daily SOM pool C mass (kgC/m2)", "kgC/m2", [
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
        ]);

        AddPatchOutput(builder, "dave_sompool_anmass", "Annual SOM Pool N Mass", "Daily SOM pool N mass (kgN/m2)", "kgN/m2", [
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
        ]);

        // Nitrogen deposition and fixation
        AddPatchOutput(builder, "dave_andep", "Annual N Deposition", "Annual nitrogen deposition (kgN/m2)", "kgN/m2", [
            "dNO3dep",
            "dNH4dep",
            "nfert",
            "total"
        ]);

        AddPatchOutput(builder, "dave_anfixation", "Annual N Fixation", "Total annual biological N fixation (kgN/m2)", "kgN/m2", [
            "nfixation"
        ]);

        // Daily patch-level outputs
        AddPatchOutput(builder, "dave_daylength", "Daylength", "Daily patch-level day-length (h)", "h", ["daylength"]);
        AddPatchOutput(builder, "dave_soil_nmass_avail", "Available Soil N", "Available Soil N for plant uptake (kgN/m2)", "kgN/m2", ["soil_nmass_avail"]);
        AddPatchOutput(builder, "dave_dsimfire", "Daily Simfire Analysis", "Daily simfire analysis", [
            ("burned_area", "fraction"),
            ("fire_carbon", "gC/m2") 
        ]);

        AddPatchOutput(builder, "dave_met_pressure", "Met Pressure", "Daily atmospheric pressure", "kPa", ["pressure"]);
        AddPatchOutput(builder, "dave_met_co2", "Met CO2", "Daily atmospheric CO2 concentration", "ppm", ["co2"]);
        AddPatchOutput(builder, "dave_sompool_cmass", "SOM Pool C Mass", "Daily SOM pool C mass", "kgC/m2", ["cmass"]);
        AddPatchOutput(builder, "dave_sompool_nmass", "SOM Pool N Mass", "Daily SOM pool N mass", "kgN/m2", ["nmass"]);
        AddPatchOutput(builder, "dave_ninput", "N Input", "Daily nitrogen input", "kgN/m2/day", ["ninput"]);
        AddPatchOutput(builder, "dave_fpar_ff", "Forest Floor FPAR", "Daily forest floor FPAR", "0-1", ["fpar_ff"]);
        AddPatchOutput(builder, "dave_resp_heterotrophic", "Heterotrophic Respiration", "Daily heterotrophic respiration", "gC/m2/day", ["resp_h"]);
        AddPatchOutput(builder, "dave_resp", "Total Respiration", "Daily total respiration", "gC/m2/day", ["resp"]);
        AddPatchOutput(builder, "dave_gpp", "GPP", "Daily gross primary production", "gC/m2/day", ["gpp"]);
        AddPatchOutput(builder, "dave_npp", "NPP", "Daily net primary production", "gC/m2/day", ["npp"]);
        AddPatchOutput(builder, "dave_nee", "NEE", "Daily net ecosystem exchange", "gC/m2/day", ["nee"]);
        AddPatchOutput(builder, "dave_evaporation", "Evaporation", "Daily evaporation", "mm/day", ["evap"]);
        AddPatchOutput(builder, "dave_soilc", "Soil Carbon", "Daily soil carbon content", "kgC/m2", ["soilc"]);
        AddPatchOutput(builder, "dave_soiln", "Soil Nitrogen", "Daily soil nitrogen content", "kgN/m2", ["soiln"]);
        AddPatchOutput(builder, "dave_soil_nflux", "Soil N Flux", "Daily soil nitrogen flux", "kgN/m2/day", ["nflux"]);
        AddPatchOutput(builder, "dave_dfuel", "Daily Fuel Availability", "Daily blaze fuel availability (kgC/m2)", "kgC/m2", ["fuel"]);
        AddPatchOutput(builder, "dave_dcoarse_woody_debris", "Daily Coarse Woody Debris", "Daily coarse woody debris (gC/m2)", "gC/m2", ["cwd"]);
        AddPatchOutput(builder, "dave_met_temp", "Temperature", "Daily air temperature (°C)", "°C", ["temp"]);
        AddPatchOutput(builder, "dave_met_par", "PAR", "Daily PAR (J/m2/timestep)", "kJ/m2/timestep", ["par"]);
        AddPatchOutput(builder, "dave_met_vpd", "VPD", "Daily VPD (kPa)", "kPa", ["vpd"]);
        AddPatchOutput(builder, "dave_met_insol", "Insolation", "Daily insolation", "", ["insol"]);
        AddPatchOutput(builder, "dave_met_precip", "Precipitation", "Daily total precipitation (mm)", "mm", ["precip"]);

        // Annual aggregated individual-level outputs
        AddPatchOutput(builder, "dave_alai", "Annual LAI", "Annual LAI(m2/m2)", "m2/m2", ["lai"]);
        AddPatchOutput(builder, "dave_afpc", "Annual FPC", "Annual FPC ()", "", ["fpc"]);
        AddPatchOutput(builder, "dave_acmass", "Annual C Mass", "Annual cmass (kgC/m2)", "kgC/m2", ["cmass"]);
        AddPatchOutput(builder, "dave_anmass", "Annual N Mass", "Annual nmass (kgN/m2)", "kgN/m2", ["nmass"]);
        AddPatchOutput(builder, "dave_aheight", "Annual Height", "Annual plant height (m)", "m", ["height"]);
        AddPatchOutput(builder, "dave_aaet", "Annual AET", "Annual actual evapotranspiration (mm)", "mm", ["aet"]);
        AddPatchOutput(builder, "dave_adensity", "Annual Density", "Annual density of individuals over patch (/m2)", "/m2", ["density"]);
        AddPatchOutput(builder, "dave_altor", "Annual Leaf to Root Ratio", "Annual leaf to root ratio (unitless)", "", ["ltor"]);
        AddPatchOutput(builder, "dave_anuptake", "Annual N Uptake", "Total annual nitrogen uptake (kgN/m2/year)", "kgN/m2/year", ["nuptake"]);
        AddPatchOutput(builder, "dave_a_aboveground_cmass", "Annual Aboveground C Mass", "Annual total above-ground C biomass (kgC/m2)", "kgC/m2", ["aboveground_cmass"]);
        AddPatchOutput(builder, "dave_a_belowground_cmass", "Annual Belowground C Mass", "Annual total below-ground C biomass (kgC/m2)", "kgC/m2", ["belowground_cmass"]);
        AddPatchOutput(builder, "dave_a_aboveground_nmass", "Annual Aboveground N Mass", "Annual total above-ground N biomass (kgN/m2)", "kgN/m2", ["aboveground_nmass"]);
        AddPatchOutput(builder, "dave_a_belowground_nmass", "Annual Belowground N Mass", "Annual total below-ground N biomass (kgN/m2)", "kgN/m2", ["belowground_nmass"]);

        // Annual PFT-level outputs
        AddPatchOutput(builder, "dave_anpp", "Annual NPP", "Total individual-level Annual NPP (kgC/m2/year)", "kgC/m2/year", ["npp"]);
        AddPatchOutput(builder, "dave_agpp", "Annual GPP", "Total individual-level Annual GPP (kgC/m2/year)", "kgC/m2/year", ["gpp"]);
        AddPatchOutput(builder, "dave_aresp", "Annual Respiration", "Total individual-level Annual autotrophic Respiration (kgC/m2/year)", "kgC/m2/year", ["resp"]);
        AddPatchOutput(builder, "dave_acmass_mort", "Annual Mortality C Mass", "Mass of annual killed vegetation (kgC/m2/year)", "kgC/m2/year", ["cmass_mort"]);
        AddPatchOutput(builder, "dave_aclitter", "Annual C Litter", "Total individual-level Annual GPP (kgC/m2/year)", "kgC/m2/year", ["clitter"]);
        AddPatchOutput(builder, "dave_ancohort", "Annual Cohort Count", "Get the number of cohorts of this PFT currently established in this patch", "count", ["ncohort"]);
        AddPatchOutput(builder, "dave_anetps_ff", "Annual Net Photosynthesis", "Annual net photosynthesis at forest floor (kgC/m2)", "kgC/m2", ["netps_ff"]);

        // Annual stand-level outputs
        AddPatchOutput(builder, "dave_stand_frac", "Stand Fraction", "Fraction of the gridcell occupied by each stand", "", ["fraction"]);
        AddPatchOutput(builder, "dave_stand_type", "Stand Type", "Stand landcover types", "", ["type"]);

        // Annual carbon allocation outputs
        AddPatchOutput(builder, "dave_acalloc_leaf", "Annual C Allocation Leaf", "Total annual C allocation to leaf (kgC/m2)", "kgC/m2", ["calloc_leaf"]);
        AddPatchOutput(builder, "dave_acalloc_root", "Annual C Allocation Root", "Total annual C allocation to root (kgC/m2)", "kgC/m2", ["calloc_root"]);
        AddPatchOutput(builder, "dave_acalloc_repr", "Annual C Allocation Repr", "Total annual C allocation to repr (kgC/m2)", "kgC/m2", ["calloc_repr"]);
        AddPatchOutput(builder, "dave_acalloc_sap", "Annual C Allocation Sap", "Total annual C allocation to sap (kgC/m2)", "kgC/m2", ["calloc_sap"]);
        AddPatchOutput(builder, "dave_acalloc_crown", "Annual C Allocation Crown", "Total annual C allocation to crown (kgC/m2)", "kgC/m2", ["calloc_crown"]);

        // Annual trunk outputs
        AddTrunkPftOutput(builder, "cmass", "C Mass", "Total carbon biomass (kgC/m2)", "kgC/m2");
        AddTrunkPftOutput(builder, "anpp", "Annual NPP", "Annual Net Primary Production (kgC/m2/year)", "kgC/m2/year");
        AddTrunkPftOutput(builder, "agpp", "Annual GPP", "Annual Gross Primary Production (kgC/m2/year)", "kgC/m2/year");
        AddTrunkPftOutput(builder, "fpc", "Foliage Projective Cover", "Foliage Projective Cover (fraction)", "0-1");
        AddTrunkPftOutput(builder, "aaet", "Annual AET", "Annual Actual Evapotranspiration (mm/year)", "mm/year");
        AddTrunkPftOutput(builder, "lai", "Leaf Area Index", "Leaf Area Index (m2/m2)", "m2/m2");
        AddTrunkOutput(builder, "cflux", "Carbon Fluxes", "Annual carbon fluxes (kgC/m2/year)", "kgC/m2/year", new[] {
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
        });

        AddTrunkPftOutput(builder, "doc", "Dissolved Organic Carbon", "Dissolved organic carbon (kgC/m2)", "kgC/m2");
        AddTrunkPftOutput(builder, "dens", "Tree Density", "Tree density (indiv/m2)", "indiv/m2");

        AddTrunkOutput(builder, "cpool", "Carbon Pools", "Soil carbon pools (kgC/m2)", "kgC/m2", new[] {
            "VegC",
            "LitterC",
            "SoilC",
            "Total"
        });

        AddTrunkPftOutput(builder, "clitter", "Carbon Litter", "Carbon in litter (kgC/m2)", "kgC/m2");

        AddTrunkOutput(builder, "tot_runoff", "Runoff", "Total runoff (mm/year)", "mm/year", new[] {
            "Surf",
            "Drain",
            "Base",
            "Total"});

        AddTrunkOutput(builder, "wetland_water_added", "Wetland Water Added", "Water added to wetland (mm)", "mm", ["H2OAdded"]);
        AddTrunkPftOutput(builder, "height", "Species Height", "Mean Species Height", "m");
        AddTrunkPftOutput(builder, "file_speciesdiam", "Species Diameter", "Mean species diameter", "m");
        AddTrunkOutput(builder, "firert", "Fire Return Time", "Fire return time", [
            ("FireRT", "years"),
            ("BurntFr", "0-1")]);

        // Nitrogen-related outputs
        AddTrunkPftOutput(builder, "nmass", "N Mass", "Total nitrogen in biomass (kgN/m2)", "kgN/m2");
        AddTrunkPftOutput(builder, "cton_leaf", "Leaf C:N Ratio", "Carbon to Nitrogen ratio in leaves", "");
        AddTrunkOutput(builder, "nsources", "N Sources", "Annual nitrogen sources (kgN/m2/year)", "kgN/m2/year", [
            "NH4dep",
            "NO3dep",
            "fix",
            "fert",
            "input",
            "min",
            "imm",
            "netmin",
            "Total"
        ]);

        AddTrunkOutput(builder, "npool", "N Pools", "Soil nitrogen pools (kgN/m2)", "kgN/m2", [
            "VegN",
            "LitterN",
            "SoilN",
            "Total"
        ]);

        AddTrunkPftOutput(builder, "nlitter", "N Litter", "Nitrogen in litter (kgN/m2)", "kgN/m2");
        AddTrunkPftOutput(builder, "nuptake", "N Uptake", "Annual nitrogen uptake (kgN/m2/year)", "kgN/m2/year");
        AddTrunkPftOutput(builder, "vmaxnlim", "Vmax N Limitation", "Annual nitrogen limitation on Vmax", "");
        
        AddTrunkOutput(builder, "nflux", "N Fluxes", "Annual nitrogen fluxes (kgN/m2/year)", "kgN/m2/year", [
            "NH4dep",
            "NO3dep",
            "fix",
            "fert",
            "est",
            "flux",
            "leach",
            "NEE",
            "Total"
        ]);

        AddTrunkOutput(builder, "ngases", "N Gas Emissions", "Annual nitrogen gas emissions (kgN/m2/year)", "kgN/m2/year", [
            "NH3_fire",
            "NH3_soil",
            "NOx_fire",
            "NOx_soil",
            "N2O_fire",
            "N2O_soil",
            "N2_fire",
            "N2_soil",
            "Total"
        ]);

        // Monthly trunk outputs
        AddMonthlyTrunkOutput(builder, "mnpp", "Monthly NPP", "Monthly Net Primary Production (kgC/m2/month)", "kgC/m2/month");
        AddMonthlyTrunkOutput(builder, "mlai", "Monthly LAI", "Monthly Leaf Area Index (m2/m2)", "m2/m2");
        AddMonthlyTrunkOutput(builder, "mgpp", "Monthly GPP", "Monthly Gross Primary Production (kgC/m2/month)", "kgC/m2/month");
        AddMonthlyTrunkOutput(builder, "mra", "Monthly Ra", "Monthly autotrophic respiration (kgC/m2/month)", "kgC/m2/month");
        AddMonthlyTrunkOutput(builder, "maet", "Monthly AET", "Monthly Actual Evapotranspiration (mm/month)", "mm/month");
        AddMonthlyTrunkOutput(builder, "mpet", "Monthly PET", "Monthly Potential Evapotranspiration (mm/month)", "mm/month");
        AddMonthlyTrunkOutput(builder, "mevap", "Monthly Evap", "Monthly Evaporation (mm/month)", "mm/month");
        AddMonthlyTrunkOutput(builder, "mrunoff", "Monthly Runoff", "Monthly runoff (mm/month)", "mm/month");
        AddMonthlyTrunkOutput(builder, "mintercep", "Monthly Interception", "Monthly interception (mm/month)", "mm/month");
        AddMonthlyTrunkOutput(builder, "mrh", "Monthly Rh", "Monthly heterotrophic respiration (kgC/m2/month)", "kgC/m2/month");
        AddMonthlyTrunkOutput(builder, "mnee", "Monthly NEE", "Monthly Net Ecosystem Exchange (kgC/m2/month)", "kgC/m2/month");
        AddMonthlyTrunkOutput(builder, "mwcont_upper", "Monthly Upper Water Content", "Monthly upper soil water content (fraction)", "");
        AddMonthlyTrunkOutput(builder, "mwcont_lower", "Monthly Lower Water Content", "Monthly lower soil water content (fraction)", "");
        AddMonthlyTrunkOutput(builder, "miso", "Monthly Isoprene", "Monthly isoprene flux (mgC/m2/month)", "mgC/m2/month");
        AddMonthlyTrunkOutput(builder, "mmon", "Monthly Monoterpene", "Monthly monoterpene flux (mgC/m2/month)", "mgC/m2/month");

        // BVOC outputs
        AddTrunkPftOutput(builder, "aiso", "Annual Isoprene", "Annual isoprene flux (mgC/m2/year)", "mgC/m2/year");
        AddTrunkPftOutput(builder, "amon", "Annual Monoterpene", "Annual monoterpene flux (mgC/m2/year)", "mgC/m2/year");
        AddTrunkPftOutput(builder, "amon_mt1", "Annual Endocyclic Monoterpene", "Annual endocyclic monoterpene flux (mgC/m2/year)", "mgC/m2/year");

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
    /// Register metadata for a PFT-level output file.
    /// </summary>
    /// <param name="builder">The dictionary builder.</param>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out).</param>
    /// <param name="name">The name of the output file (e.g., "Leaf Area Index").</param>
    /// <param name="description">A description of the output file (e.g., "Annual Leaf Area Index").</param>
    /// <param name="units">The units of the output file (e.g., "m2/m2").</param>
    private static void AddPftOutput(ImmutableDictionary<string, OutputFileMetadata>.Builder builder, string fileType, string name, string description, string units)
    {
        builder.Add(fileType, new OutputFileMetadata(
            fileName: fileType,
            name: name,
            description: description,
            layers: new PftLayers(new Unit(units))));
    }

    /// <summary>
    /// Register metadata for an individual-level output file, in which all
    /// layers have the same units.
    /// </summary>
    /// <param name="builder">The dictionary builder.</param>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out).</param>
    /// <param name="name">The name of the output file (e.g., "Leaf Area Index").</param>
    /// <param name="description">A description of the output file (e.g., "Annual Leaf Area Index").</param>
    /// <param name="units">The units of the output file (e.g., "m2/m2").</param>
    /// <param name="layers">A list of layer names in the output file.</param>
    private static void AddIndivOutput(ImmutableDictionary<string, OutputFileMetadata>.Builder builder, string fileType, string name, string description, string units, string[] layers)
    {
        builder.Add(fileType, new OutputFileMetadata(
            fileName: fileType,
            name: name,
            description: description,
            layers: new IndividualLayers(layers.Select(l => (l, new Unit(units))).ToArray())));
    }

    /// <summary>
    /// Register metadata for an individual-level output file, in which all
    /// layers have different units.
    /// </summary>
    /// <param name="builder">The dictionary builder.</param>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out).</param>
    /// <param name="name">The name of the output file (e.g., "Leaf Area Index").</param>
    /// <param name="description">A description of the output file (e.g., "Annual Leaf Area Index").</param>
    /// <param name="layers">A list of pairs of (layer name, units).</param>
    private static void AddIndivOutput(ImmutableDictionary<string, OutputFileMetadata>.Builder builder, string fileType, string name, string description, (string, string)[] layers)
    {
        builder.Add(fileType, new OutputFileMetadata(
            fileName: fileType,
            name: name,
            description: description,
            layers: new IndividualLayers(layers.Select(l => (l.Item1, new Unit(l.Item2))).ToArray())));
    }

    /// <summary>
    /// Register metadata for a patch-level output file.
    /// </summary>
    /// <param name="builder">The dictionary builder.</param>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out).</param>
    /// <param name="name">The name of the output file (e.g., "Leaf Area Index").</param>
    /// <param name="description">A description of the output file (e.g., "Annual Leaf Area Index").</param>
    /// <param name="layers">A list of pairs of (layer name, units).</param>
    private static void AddPatchOutput(ImmutableDictionary<string, OutputFileMetadata>.Builder builder, string fileType, string name, string description, (string layer, string units)[] layers)
    {
        builder.Add(fileType, new OutputFileMetadata(
            fileName: fileType,
            name: name,
            description: description,
            layers: new DaveLayers(layers.Select(l => (l.layer, new Unit(l.units))).ToArray())));
    }

    /// <summary>
    /// Register metadata for a patch-level output file in which all data
    /// columns have the same units.
    /// </summary>
    /// <param name="builder">The dictionary builder.</param>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out).</param>
    /// <param name="name">The name of the output file (e.g., "Leaf Area Index").</param>
    /// <param name="description">A description of the output file (e.g., "Annual Leaf Area Index").</param>
    /// <param name="units">The units of all columns in the output file (e.g., "m2/m2").</param>
    /// <param name="layers">A list of layer names in the output file.</param>
    private static void AddPatchOutput(ImmutableDictionary<string, OutputFileMetadata>.Builder builder, string fileType, string name, string description, string units, string[] layers)
    {
        builder.Add(fileType, new OutputFileMetadata(
            fileName: fileType,
            name: name,
            description: description,
            layers: new DaveLayers(layers.Select(l => (l, new Unit(units))).ToArray())));
    }

    /// <summary>
    /// Register metadata for a trunk output file.
    /// </summary>
    /// <param name="builder">The dictionary builder.</param>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out).</param>
    /// <param name="name">The name of the output file (e.g., "Leaf Area Index").</param>
    /// <param name="description">A description of the output file (e.g., "Annual Leaf Area Index").</param>
    /// <param name="units">The units of all columns in the output file (e.g., "m2/m2").</param>
    /// <param name="layers">A list of layer names in the output file.</param>
    private static void AddTrunkOutput(ImmutableDictionary<string, OutputFileMetadata>.Builder builder, string fileType, string name, string description, string units, string[] layers)
    {
        builder.Add(fileType, new OutputFileMetadata(
            fileName: fileType,
            name: name,
            description: description,
            layers: new TrunkLayers(layers.Select(l => (l, new Unit(units))).ToArray())));
    }

    /// <summary>
    /// Register metadata for a trunk output file.
    /// </summary>
    /// <param name="builder">The dictionary builder.</param>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out).</param>
    /// <param name="name">The name of the output file (e.g., "Leaf Area Index").</param>
    /// <param name="description">A description of the output file (e.g., "Annual Leaf Area Index").</param>
    /// <param name="layers">A list of pairs of (layer name, units).</param>
    private static void AddTrunkOutput(ImmutableDictionary<string, OutputFileMetadata>.Builder builder, string fileType, string name, string description, (string layer, string units)[] layers)
    {
        builder.Add(fileType, new OutputFileMetadata(
            fileName: fileType,
            name: name,
            description: description,
            layers: new TrunkLayers(layers.Select(l => (l.layer, new Unit(l.units))).ToArray())));
    }

    /// <summary>
    /// Register metadata for a trunk output file.
    /// </summary>
    /// <param name="builder">The dictionary builder.</param>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out).</param>
    /// <param name="name">The name of the output file (e.g., "Leaf Area Index").</param>
    /// <param name="description">A description of the output file (e.g., "Annual Leaf Area Index").</param>
    /// <param name="units">The units of all columns in the output file (e.g., "m2/m2").</param>
    private static void AddTrunkPftOutput(ImmutableDictionary<string, OutputFileMetadata>.Builder builder, string fileType, string name, string description, string units)
    {
        builder.Add(fileType, new OutputFileMetadata(
            fileName: fileType,
            name: name,
            description: description,
            layers: new TrunkPftLayers(new Unit(units))));
    }

    /// <summary>
    /// Register metadata for a monthly trunk output file.
    /// </summary>
    /// <param name="builder">The dictionary builder.</param>
    /// <param name="fileType">The type of output file (e.g., "lai" for lai.out).</param>
    /// <param name="name">The name of the output file (e.g., "Leaf Area Index").</param>
    /// <param name="description">A description of the output file (e.g., "Annual Leaf Area Index").</param>
    /// <param name="units">The units of all columns in the output file (e.g., "m2/m2").</param>
    private static void AddMonthlyTrunkOutput(ImmutableDictionary<string, OutputFileMetadata>.Builder builder, string fileType, string name, string description, string units)
    {
        string[] cols = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "Total"];
        builder.Add(fileType, new OutputFileMetadata(
            fileName: fileType,
            name: name,
            description: description,
            layers: new TrunkLayers(cols.Select(c => (c, new Unit(units))).ToArray())));
    }
}
