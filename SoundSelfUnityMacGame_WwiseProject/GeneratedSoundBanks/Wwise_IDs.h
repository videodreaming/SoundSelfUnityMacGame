/////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Audiokinetic Wwise generated include file. Do not edit.
//
/////////////////////////////////////////////////////////////////////////////////////////////////////

#ifndef __WWISE_IDS_H__
#define __WWISE_IDS_H__

#include <AK/SoundEngine/Common/AkTypes.h>

namespace AK
{
    namespace EVENTS
    {
        static const AkUniqueID PLAY_AVSGENERATOR = 2590479921U;
        static const AkUniqueID PLAY_INTERACTIVEMUSICSYSTEM_SILENTLOOPS = 2845481429U;
        static const AkUniqueID PLAY_LINEARMUSIC_OPENING = 2675524623U;
        static const AkUniqueID PLAY_VO_OPENING = 1386597262U;
    } // namespace EVENTS

    namespace STATES
    {
        namespace GAMEMODE
        {
            static const AkUniqueID GROUP = 261089142U;

            namespace STATE
            {
                static const AkUniqueID ADJUNCT = 2534402132U;
                static const AkUniqueID INTEGRATION = 4094247797U;
                static const AkUniqueID NONE = 748895195U;
                static const AkUniqueID PASSIVE = 2595709632U;
                static const AkUniqueID PREPARATION = 3147549296U;
                static const AkUniqueID WISDOM = 4033993124U;
            } // namespace STATE
        } // namespace GAMEMODE

    } // namespace STATES

    namespace SWITCHES
    {
        namespace INTERACTIVEMUSICSWITCHGROUP
        {
            static const AkUniqueID GROUP = 2420200949U;

            namespace SWITCH
            {
                static const AkUniqueID B = 84696445U;
                static const AkUniqueID C = 84696444U;
                static const AkUniqueID E = 84696442U;
                static const AkUniqueID G = 84696440U;
            } // namespace SWITCH
        } // namespace INTERACTIVEMUSICSWITCHGROUP

        namespace LINEARMUSIC_OPENING
        {
            static const AkUniqueID GROUP = 611900938U;

            namespace SWITCH
            {
                static const AkUniqueID OPENINGLONG = 2367398737U;
                static const AkUniqueID OPENINGSHORT = 3060490183U;
            } // namespace SWITCH
        } // namespace LINEARMUSIC_OPENING

        namespace VO_CLOSINGGOODBYE
        {
            static const AkUniqueID GROUP = 1815817025U;

            namespace SWITCH
            {
                static const AkUniqueID ANCHORING = 1927133646U;
                static const AkUniqueID GRATITUDE = 1721781634U;
                static const AkUniqueID LONG = 674228435U;
                static const AkUniqueID SHORT = 2585211341U;
                static const AkUniqueID SILENCE = 3041563226U;
            } // namespace SWITCH
        } // namespace VO_CLOSINGGOODBYE

        namespace VO_OPENING
        {
            static const AkUniqueID GROUP = 4212583561U;

            namespace SWITCH
            {
                static const AkUniqueID OPENINGLONG = 2367398737U;
                static const AkUniqueID OPENINGPASSIVE = 335023706U;
                static const AkUniqueID OPENINGSHORT = 3060490183U;
            } // namespace SWITCH
        } // namespace VO_OPENING

        namespace VO_POSTURE
        {
            static const AkUniqueID GROUP = 184547165U;

            namespace SWITCH
            {
                static const AkUniqueID LIEDOWN = 3891673577U;
                static const AkUniqueID RELAX = 3551080421U;
            } // namespace SWITCH
        } // namespace VO_POSTURE

        namespace VO_SOMATIC
        {
            static const AkUniqueID GROUP = 3813349969U;

            namespace SWITCH
            {
                static const AkUniqueID LONG = 674228435U;
                static const AkUniqueID SHORT = 2585211341U;
            } // namespace SWITCH
        } // namespace VO_SOMATIC

        namespace VO_THEMATICCONTENT
        {
            static const AkUniqueID GROUP = 3925841397U;

            namespace SWITCH
            {
                static const AkUniqueID DIEWELL = 2721896533U;
                static const AkUniqueID NARRATIVE = 1047870521U;
                static const AkUniqueID PEACE = 103389341U;
                static const AkUniqueID SURRENDER = 2965941853U;
            } // namespace SWITCH
        } // namespace VO_THEMATICCONTENT

        namespace VO_THEMATICSAVASANA
        {
            static const AkUniqueID GROUP = 4155508698U;

            namespace SWITCH
            {
                static const AkUniqueID ANCHORING = 1927133646U;
                static const AkUniqueID FIREFLIES = 1126065684U;
                static const AkUniqueID GRATITUDE = 1721781634U;
                static const AkUniqueID KINDNESS = 2655751454U;
                static const AkUniqueID LONG = 674228435U;
                static const AkUniqueID METTA = 2692078280U;
                static const AkUniqueID NARRATIVE = 1047870521U;
                static const AkUniqueID PEACE = 103389341U;
                static const AkUniqueID SHORT = 2585211341U;
                static const AkUniqueID SILENCE = 3041563226U;
                static const AkUniqueID SURRENDER = 2965941853U;
            } // namespace SWITCH
        } // namespace VO_THEMATICSAVASANA

        namespace VO_WISDOM
        {
            static const AkUniqueID GROUP = 683317262U;

            namespace SWITCH
            {
                static const AkUniqueID DIEWELL = 2721896533U;
                static const AkUniqueID JAGUAR = 1398814679U;
                static const AkUniqueID NOTKNOWING = 1057915111U;
                static const AkUniqueID PERU = 1743886715U;
            } // namespace SWITCH
        } // namespace VO_WISDOM

    } // namespace SWITCHES

    namespace GAME_PARAMETERS
    {
        static const AkUniqueID AVSMASTERVOLUME = 1426024143U;
        static const AkUniqueID BLUESINEVOLUME = 1417631880U;
        static const AkUniqueID FUNDAMENTALSILENTVOLUME = 2144346671U;
        static const AkUniqueID GREENSINEVOLUME = 71194751U;
        static const AkUniqueID HARMONYSILENTVOLUME = 1111564044U;
        static const AkUniqueID INTERACTIVEMUSICSILENTLOOPS = 2108330916U;
        static const AkUniqueID REDSINEVOLUME = 1546458207U;
        static const AkUniqueID VO_BUS_VOLUME = 3687478936U;
    } // namespace GAME_PARAMETERS

    namespace BANKS
    {
        static const AkUniqueID INIT = 1355168291U;
        static const AkUniqueID MAIN = 3161908922U;
    } // namespace BANKS

    namespace BUSSES
    {
        static const AkUniqueID AVS_SYSTEM = 65973818U;
        static const AkUniqueID MASTER_AUDIO_BUS = 3803692087U;
        static const AkUniqueID VO_BUS = 2018813558U;
    } // namespace BUSSES

    namespace AUDIO_DEVICES
    {
        static const AkUniqueID NO_OUTPUT = 2317455096U;
        static const AkUniqueID SYSTEM = 3859886410U;
    } // namespace AUDIO_DEVICES

}// namespace AK

#endif // __WWISE_IDS_H__
