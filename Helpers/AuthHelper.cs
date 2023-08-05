using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using DotNetAPI.Data;
using DotNetAPI.Dto;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

namespace DotNetAPI.Helpers
{
    public class AuthHelper
    {
        private readonly DataContextDapper _dapper;
        private readonly IConfiguration _config;
        public AuthHelper(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
            _config = config;   
        }
        public byte[] GetPasswordHash(string password, byte[] passwordSalt)
        {
            string passwordSaltPlusString = _config.GetSection("AppSettings:PasswordKey")
                .Value + Convert.ToBase64String(passwordSalt);

            return KeyDerivation.Pbkdf2(
                password: password,
                salt: Encoding.ASCII.GetBytes(passwordSaltPlusString),
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8
            );
        }

        public string CreateToken(int userId)
        {
            Claim[] claims = new Claim[] {
                new Claim("userId", userId.ToString())
            };

            //SymmetricSecurityKey tokenKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("Appsettings:TokenKey").Value));
            string? tokenKeyString = _config.GetSection("AppSettings:TokenKey").Value;
 
            SymmetricSecurityKey tokenKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        tokenKeyString != null ? tokenKeyString : ""
                    )
                );

            SigningCredentials credentials = new SigningCredentials(tokenKey, SecurityAlgorithms.HmacSha512);

            SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor()
                {
                    Subject = new ClaimsIdentity(claims),
                    SigningCredentials = credentials,
                    Expires = DateTime.Now.AddDays(1)
                };

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            SecurityToken token = tokenHandler.CreateToken(descriptor);

            return tokenHandler.WriteToken(token);
        }

        public bool SetPassword(UserForLoginDto userForSetPassword)
        {
            byte[] passwordSalt = new byte[128 / 8];
                    using(RandomNumberGenerator rng = RandomNumberGenerator.Create()) 
                    {
                        rng.GetNonZeroBytes(passwordSalt);
                    }

                    byte[] passwordHash = GetPasswordHash(userForSetPassword.Password, passwordSalt);

                    string sqlAddAuth = @"
                        EXEC TutorialAppSchema.spRegistration_Upsert
                        @Email = @EmailParam, 
                        @PasswordHash = @PasswordHashParam, 
                        @PasswordSalt = @PasswordSaltParam";


                    DynamicParameters sqlParameters = new DynamicParameters();

                    // SqlParameter emailParameter = new SqlParameter("@EmailParam", SqlDbType.VarBinary);
                    // emailParameter.Value = userForLogin.Email;
                    // sqlParameters.Add(emailParameter);
                    sqlParameters.Add("@EmailParam", userForSetPassword.Email, DbType.String);
                    sqlParameters.Add("@PasswordHashParam", passwordHash, DbType.Binary); //Binary is expected for a VARBTYEARRAY
                    sqlParameters.Add("@PasswordSaltParam", passwordSalt, DbType.Binary);
                    
                    

                    return _dapper.ExecuteSQLWithParameters(sqlAddAuth, sqlParameters);
                    
                    
        }
    }
}