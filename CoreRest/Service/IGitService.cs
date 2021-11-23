using CoreRest.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreRest.Service
{
    public interface IGitService
    {
        LoginData Auth(string token);
        UserData GetUser(string user);
        List<ProjectModel> GetStarredProjectsForUser(string user);
        List<ProjectModel> GetCorrectRepositoriesData(string token);
        ProjectsSearch FindProjectsByTopics(List<string> topics);
        void UnstarProject(string token, string owner, string repo);
        void StarProject(string token, string owner, string repo);
    }
}