using Cryptforge.Core;

namespace Cryptforge.Save
{
    // The forged hero skills between their JSON field (ProfileSaveData version 6: heroSkills, with the toasts' flags) and
    // the plain SkillSnapshot ProfileSkills restores from and writes to. Copies values only; ProfileSkills.Restore does the
    // repairing, so a damaged field here is never fatal.
    public static class ProfileSkillsSave
    {
        public static SkillSnapshot ToSnapshot(ProfileSaveData data)
        {
            var snapshot = new SkillSnapshot();
            if (data == null)
                return snapshot;
            snapshot.Records = new HeroSkillRecord[data.heroSkills?.Length ?? 0];
            for (int i = 0; i < snapshot.Records.Length; i++)
            {
                HeroSkillsSaveData record = data.heroSkills[i];
                snapshot.Records[i] = record == null ? null : new HeroSkillRecord
                {
                    Hero = record.hero, SkillTwo = record.skillTwo, SkillThree = record.skillThree,
                    SkillTwoToastShown = record.skillTwoToastShown, SkillThreeToastShown = record.skillThreeToastShown
                };
            }
            return snapshot;
        }

        public static void Write(SkillSnapshot snapshot, ProfileSaveData data)
        {
            data.heroSkills = new HeroSkillsSaveData[snapshot.Records.Length];
            for (int i = 0; i < data.heroSkills.Length; i++)
            {
                HeroSkillRecord record = snapshot.Records[i];
                data.heroSkills[i] = new HeroSkillsSaveData
                {
                    hero = record.Hero, skillTwo = record.SkillTwo, skillThree = record.SkillThree,
                    skillTwoToastShown = record.SkillTwoToastShown, skillThreeToastShown = record.SkillThreeToastShown
                };
            }
        }
    }
}
