using System;
using System.Collections.Generic;

namespace mhd.Bio;
public class Bio
{
    public string id { get; set; } = string.Empty;
    public string perIdentification { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string Initial { get; set; } = string.Empty;
    public string bioSpouse { get; set; } = string.Empty;
    public string bioAddress { get; set; } = string.Empty;
    public string bioCity { get; set; } = string.Empty;
    public string bioState { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string bioZip { get; set; } = string.Empty;
    public string bioPhone { get; set; } = string.Empty;
    public string bioRank { get; set; } = string.Empty;
    public string bioRetRank { get; set; } = string.Empty;
    public string bioNickname { get; set; } = string.Empty;
    public string bioBirthDate { get; set; } = string.Empty;
    public string bioBirthCity { get; set; } = string.Empty;
    public string bioBirthState { get; set; } = string.Empty;
    public string bioDateDeceased { get; set; } = string.Empty;
    public string bioDateEnteredService { get; set; } = string.Empty;
    public string bioEntryLocation { get; set; } = string.Empty;
    public string bioEntryState { get; set; } = string.Empty;
    public string bioSpecialTraining { get; set; } = string.Empty;
    public string bioCadetClass { get; set; } = string.Empty;
    public string bioGraduationLocation { get; set; } = string.Empty;
    public string bioGraduationState { get; set; } = string.Empty;
    public string bioLocationArrivalDate { get; set; } = string.Empty;
    public string bioArrivedFrom { get; set; } = string.Empty;
    public string bioArrivedHow { get; set; } = string.Empty;
    public string bioTripDetails { get; set; } = string.Empty;
    public string bioDepartedLocation { get; set; } = string.Empty;
    public string bioDepartedFrom { get; set; } = string.Empty;
    public string bioDepartedHow { get; set; } = string.Empty;
    public string bioTripHomeDetails { get; set; } = string.Empty;
    public string bioMOH { get; set; } = string.Empty;
    public string bioDSC { get; set; } = string.Empty;
    public string bioDSM { get; set; } = string.Empty;
    public string bioSilverStar { get; set; } = string.Empty;
    public string bioLegionOfMerit { get; set; } = string.Empty;
    public string bioDFC { get; set; } = string.Empty;
    public string bioSoldiersMedal { get; set; } = string.Empty;
    public string bioBronzStar { get; set; } = string.Empty;
    public string bioAirMedal { get; set; } = string.Empty;
    public string bioPurpleHart { get; set; } = string.Empty;
    public string bioUnitCitation { get; set; } = string.Empty;
    public string bioetoservice { get; set; } = string.Empty;
    public string biowwiivictory { get; set; } = string.Empty;
    public string bioPOW_Medal { get; set; } = string.Empty;
    public string bioGoodConduct { get; set; } = string.Empty;
    public string bioOtherAwards { get; set; } = string.Empty;
    public string bioAwardsNotReceived { get; set; } = string.Empty;
    public string bioKorea { get; set; } = string.Empty;
    public string bioVietnam { get; set; } = string.Empty;
    public string bioOtherService { get; set; } = string.Empty;
    public string bioActiveReserveYears { get; set; } = string.Empty;
    public string bioOccupationPrior { get; set; } = string.Empty;
    public string bioOccupationAfter { get; set; } = string.Empty;
    public string bioFlightCrewPosition { get; set; } = string.Empty;
    public string bioSerialNoFlightCrew { get; set; } = string.Empty;
    public string bioSquadronFlight { get; set; } = string.Empty;
    public string bioTotalMissionsFlown { get; set; } = string.Empty;
    public string bioFirstMissionDate { get; set; } = string.Empty;
    public string bioLastMissionDate { get; set; } = string.Empty;
    public string bioMemories { get; set; } = string.Empty;
    public string bioShotDown { get; set; } = string.Empty;
    public string bioEvadedCapture { get; set; } = string.Empty;
    public string bioPOW { get; set; } = string.Empty;
    public string bioMIA { get; set; } = string.Empty;
    public string bioKIA { get; set; } = string.Empty;
    public string bioDitched { get; set; } = string.Empty;
    public string bioCrashed { get; set; } = string.Empty;
    public string bioInterned { get; set; } = string.Empty;
    public string bioEscaped { get; set; } = string.Empty;
    public string bioParachuted { get; set; } = string.Empty;
    public string bioWounded { get; set; } = string.Empty;
    public string bioHospitalized { get; set; } = string.Empty;
    public string bioDeceased { get; set; } = string.Empty;
    public string bioOther { get; set; } = string.Empty;
    public string bioDetails { get; set; } = string.Empty;
    public string bioMemorabilia { get; set; } = string.Empty;
    public string bioGroundCrewDuties { get; set; } = string.Empty;
    public string bioSerialNoGroundCrew { get; set; } = string.Empty;
    public string bioSquadronGround { get; set; } = string.Empty;
    public string bioOtherDutiesGroundCrew { get; set; } = string.Empty;
    public string bioAircraftCrewed { get; set; } = string.Empty;
    public string bioAircraftDamage { get; set; } = string.Empty;
    public string bioAircraftLost { get; set; } = string.Empty;
    public string bioGroundCrewMemories { get; set; } = string.Empty;
    public string bioFriendsMemoriesExperiences { get; set; } = string.Empty;
    public string bioBaseOperationAssignment { get; set; } = string.Empty;
    public string bioSerialNoBaseOperation { get; set; } = string.Empty;
    public string bioSquadronBaseOps { get; set; } = string.Empty;
    public string bioOtherDutiesBaseOps { get; set; } = string.Empty;
    public string bioMemoriesDetails { get; set; } = string.Empty;
    public string bioAfterYear { get; set; } = string.Empty;
    public string bioBeforeYear { get; set; } = string.Empty;
    public string bioAudioClip { get; set; } = string.Empty;
    public string bioAdministration { get; set; } = string.Empty;
    public string bioClerical { get; set; } = string.Empty;
    public string bioCommunications { get; set; } = string.Empty;
    public string bioEngineering { get; set; } = string.Empty;
    public string bioIntelligence { get; set; } = string.Empty;
    public string bioMedical { get; set; } = string.Empty;
    public string bioMessHall { get; set; } = string.Empty;
    public string bioPhotoLab { get; set; } = string.Empty;
    public string bioSecurity { get; set; } = string.Empty;
    public string bioSupply { get; set; } = string.Empty;
    public string bioWeather { get; set; } = string.Empty;
    public string bioGSDother { get; set; } = string.Empty;
    public string bioAEMechanic { get; set; } = string.Empty;
    public string bioArmorer { get; set; } = string.Empty;
    public string bioAssistantCC { get; set; } = string.Empty;
    public string bioBombsight { get; set; } = string.Empty;
    public string bioCrewChief { get; set; } = string.Empty;
    public string bioHydraulics { get; set; } = string.Empty;
    public string bioInstruments { get; set; } = string.Empty;
    public string bioRadar { get; set; } = string.Empty;
    public string bioRadio { get; set; } = string.Empty;
    public string bioRefueling { get; set; } = string.Empty;
    public string bioSheetMetal { get; set; } = string.Empty;
    public string bioGCDother { get; set; } = string.Empty;
    public string bioOtherSquadrons { get; set; } = string.Empty;
    public string bioOtherGroups { get; set; } = string.Empty;
    public string BioDate { get; set; } = string.Empty;
}

public class BioSummary
{
    public BioSummary()
    {

    }
    public BioSummary(Bio Bio)
    {
        Id = Bio.id;
        PerIdentification = Bio.perIdentification;
        LastName = Bio.LastName;
        FirstName = Bio.FirstName;
    }
    public string Id { get; set; } = string.Empty;
    public string PerIdentification { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
}

public interface IBioSummary
{
    List<BioSummary> Bio {get;}
   
}
public interface IBioService
{
     Task<List<BioSummary>> QueryDocumentAsync(
    
    );
    Task<Bio> LoadDocumentAsync(string PerIdentification);
}