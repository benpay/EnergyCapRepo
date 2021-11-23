using CoreRest.Models;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CoreRest.Service
{
    public class GitService : IGitService
    {
        private readonly IConfiguration _configuration;

        public GitService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public LoginData Auth(string token)
        {
            try
            {
                var client = new RestClient(new Uri($"{_configuration.GetValue<string>("GitApiUrl")}"));
                var request = new RestRequest($"user", Method.GET);
                request.AddHeader("Authorization", "Bearer " + token);
                var result = client.Execute<LoginData>(request);

                if (!result.IsSuccessful)
                    throw new HttpRequestException($"Error during Git auth, Method : {System.Reflection.MethodBase.GetCurrentMethod().Name}");

                return result.Data;
            }
            catch (WebException ex)
            {
                throw new WebException(ex.Message);
            }
        }

        public List<ProjectModel> GetCorrectRepositoriesData(string token)
        {
            try
            {
                var login = Auth(token);
                var projectsList = GetStarredProjectsForUser(login.login);

                foreach (var project in projectsList)
                {
                    if (IsLicenseGPL(project))
                    {
                        var newProjects = FindProjectsByTopics(project.topics.ToList());
                        if ((newProjects.total_count == 1 && newProjects.items.FirstOrDefault().id != project.id)
                            || newProjects.total_count > 1)
                        {
                            var projectUnstarred = false;
                            foreach(var projectToAdd in newProjects.items.Take(5))
                            {
                                if (!IsLicenseGPL(projectToAdd))
                                {
                                    StarProject(token, projectToAdd.owner.login, projectToAdd.name);
                                    if (!projectUnstarred)
                                    {
                                        UnstarProject(token, project.owner.login, project.name);
                                        projectUnstarred = true;
                                    }
                                }
                            }
                        }
                    }
                }

                return GetStarredProjectsForUser(login.login);
            }
            catch (WebException ex)
            {
                throw new WebException(ex.Message);
            }
        }

        public ProjectsSearch FindProjectsByTopics(List<string> topics)
        {
            try
            {
                var topicsToSearch = topics.Count() <= 3 ? String.Join("+", topics.ToArray()) : String.Join("+", topics.ToArray().Take(3));
                var client = new RestClient(new Uri($"{_configuration.GetValue<string>("GitApiUrl")}"));
                var request = new RestRequest($"search/repositories?q=topic:{topicsToSearch}", Method.GET);
                var result = client.Execute<ProjectsSearch>(request);

                if (!result.IsSuccessful)
                    throw new HttpRequestException($"Error during Git auth, Method : {System.Reflection.MethodBase.GetCurrentMethod().Name}");
                else if (result.Data.items is null)
                    throw new Exception($"Empty data returned by Gitlab for filters {topicsToSearch}");
                else if (result.Data.total_count == 0)
                    throw new ArgumentException($"No results for projects with same topics: {topicsToSearch}");

                return result.Data;
            }
            catch (WebException ex)
            {
                throw new WebException(ex.Message);
            }
        }

        public void StarProject(string token, string owner, string repo)
        {
            try
            {
                var client = new RestClient(new Uri($"{_configuration.GetValue<string>("GitApiUrl")}"));
                var request = new RestRequest($"user/starred/{owner}/{repo}", Method.PUT);
                request.AddHeader("Authorization", "Bearer " + token);
                var result = client.Execute<UserData>(request);

                if (!result.IsSuccessful)
                    throw new HttpRequestException($"Error during Git unstar project: {owner} + {repo}");
            }
            catch (WebException ex)
            {
                throw new WebException(ex.Message);
            }
        }

        public void UnstarProject(string token, string owner, string repo)
        {
            try
            {
                var client = new RestClient(new Uri($"{_configuration.GetValue<string>("GitApiUrl")}"));
                var request = new RestRequest($"user/starred/{owner}/{repo}", Method.DELETE);
                request.AddHeader("Authorization", "Bearer " + token);
                var result = client.Execute<UserData>(request);

                if (!result.IsSuccessful)
                    throw new HttpRequestException($"Error during Git star project: {owner} + {repo}");
            }
            catch (WebException ex)
            {
                throw new WebException(ex.Message);
            }
        }

        public UserData GetUser(string user)
        {
            try
            {
                var client = new RestClient(new Uri($"{_configuration.GetValue<string>("GitApiUrl")}"));
                var request = new RestRequest($"users/{user}", Method.GET);
                var result = client.Execute<UserData>(request);

                if (!result.IsSuccessful)
                    throw new HttpRequestException($"Error during Git get data, Method : {System.Reflection.MethodBase.GetCurrentMethod().Name}, GitUser : {user}");

                return result.Data;
            }
            catch (WebException ex)
            {
                throw new WebException(ex.Message);
            }
        }

        public List<ProjectModel> GetStarredProjectsForUser(string user)
        {
            try
            {
                var client = new RestClient(new Uri($"{_configuration.GetValue<string>("GitApiUrl")}"));
                var request = new RestRequest($"users/{user}/starred", Method.GET);
                var result = client.Execute<List<ProjectModel>>(request);

                if (!result.IsSuccessful)
                    throw new HttpRequestException($"Error during Git get starred projects, Method : {System.Reflection.MethodBase.GetCurrentMethod().Name}, GitUser : {user}");

                return result.Data;
            }
            catch (WebException ex)
            {
                throw new WebException(ex.Message);
            }
        }

        private bool IsLicenseGPL(ProjectModel project)
        {
            return project.license.key.ToLower().Contains("gpl");
        }
    }
}
