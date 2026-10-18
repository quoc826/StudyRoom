using System.Collections.Generic;

public static class StudyRoomDailyQuests
{
    public enum Goal { Visit, OnlineTime, StudyTime }

    public sealed class Quest
    {
        public string Title { get; }
        public Goal Type { get; }
        public double TargetSeconds { get; }
        public int Coins { get; }

        public Quest(string title, Goal type, double targetSeconds, int coins)
        { Title = title; Type = type; TargetSeconds = targetSeconds; Coins = coins; }
    }

    public static IReadOnlyList<Quest> All { get; } = new List<Quest>
    {
        new Quest("Điểm danh hôm nay", Goal.Visit, 1, 20),
        new Quest("Học bài trong 30 phút", Goal.StudyTime, 30 * 60, 35),
        new Quest("Ở trong game 60 phút", Goal.OnlineTime, 60 * 60, 30),
        new Quest("Học tổng cộng 60 phút", Goal.StudyTime, 60 * 60, 60),
        new Quest("Thử thách trong game 2 tiếng", Goal.OnlineTime, 120 * 60, 55)
    }.AsReadOnly();
}
