using System.Data;
using Dapper;
using DotNetAPI.Data;
using DotNetAPI.Dto;
using DotNetAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class PostController : ControllerBase
    {
        private readonly DataContextDapper _dapper;

        public PostController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
        }

        [HttpGet("Posts/{postId}/{userId}/{searchParam}")]
        public IEnumerable<Post> GetPosts(int postId = 0, int userId = 0, string searchParam = "None")
        {
            string sql = @"EXEC TutorialAppSchema.spPosts_Get";
            string stringParameters = "";
            DynamicParameters sqlParameters = new DynamicParameters();

            if(postId != 0){
                stringParameters += ", @PostId=@PostIdParam";
                sqlParameters.Add("@PostIdParam", postId, DbType.Int32);
            }

            if(userId != 0){
                stringParameters += ", @UserId=@UserIdParam";
                sqlParameters.Add("@UserIdParam", userId, DbType.Int32);
            }

            if(searchParam.ToLower() != "none"){
                stringParameters += ", @SearchValue=@SearchValueParam";
                sqlParameters.Add("@SearchValueParam", searchParam, DbType.String);
            }

            if(stringParameters.Length > 0){
                sql += stringParameters.Substring(1); //cuts off the comma from the first parameter
            }

            return _dapper.LoadDataWithParameters<Post>(sql, sqlParameters);
        }


        [HttpGet("MyPosts")]
        public IEnumerable<Post> GetMyPosts()
        {
            DynamicParameters sqlParameters = new DynamicParameters();
            string sql = @"
                EXEC TutorialAppSchema.spPosts_Get 
                @UserId = @UserIdParam"; //this pulls the UserId out of the token

            sqlParameters.Add("@UserIdParam", this.User.FindFirst("userId")?.Value, DbType.Int32);
                
            return _dapper.LoadDataWithParameters<Post>(sql, sqlParameters);
        }

        [HttpPut("UpsertPost")]
        public IActionResult UpsertPost(Post postToUpsert)
        {
            DynamicParameters sqlParameters = new DynamicParameters();
            string sql = @"
                EXEC TutorialAppSchema.spPost_Upsert
                @UserId=@UserIdParam,
                @PostTitle=@PostTitleParam,
                @PostContent=@PostContentParam";

            sqlParameters.Add("@UserIdParam", this.User.FindFirst("userId")?.Value, DbType.Int32);
            sqlParameters.Add("@PostTitleParam", postToUpsert.PostTitle, DbType.String);
            sqlParameters.Add("@PostContentParam", postToUpsert.PostContent, DbType.String);

            if(postToUpsert.PostId > 0){
                sql += ", @PostId=" + postToUpsert.PostId;
                sqlParameters.Add("@PostIdParam", postToUpsert.PostId, DbType.Int32);
            }
        

            if(_dapper.ExecuteSQLWithParameters(sql, sqlParameters))
            {
                return Ok();
            }

            throw new Exception("Failed to Upsert new post!");
        }


        [HttpDelete("Post/{postId}")]
        public IActionResult DeletePost(int postId)
        {
            DynamicParameters sqlParameters = new DynamicParameters();
            string sql = @"
            EXEC TutorialAppSchema.spPost_Delete 
            @PostId=@PostIdParam 
            @UserId=@UserIdParam";

            sqlParameters.Add("@UserIdParam", this.User.FindFirst("userId")?.Value, DbType.Int32);
            sqlParameters.Add("@PostIdParam", postId, DbType.Int32);

            if(_dapper.ExecuteSQLWithParameters(sql, sqlParameters))
            {
                return Ok();
            }

            throw new Exception("Failed to delete post!");
        }

    }
}