namespace Service.Library;

public class SecretManage
{
    private static string _base_path = System.Environment.GetEnvironmentVariable("SECRET_PATH") ?? "/run/secrets/";
    private static string _lamda_key = String.Empty;
    private static string _github_token = String.Empty;
    
    public static string GetLambdaKey()
    {
        if(SecretManage._lamda_key == String.Empty)
        {
            var path = Path.Combine( _base_path, "lambda_cloude_api.txt");
            if(System.IO.File.Exists(path))
            {
                SecretManage._lamda_key = System.IO.File.ReadAllText(path).Trim();
            }
            
        }
        return SecretManage._lamda_key;
    }
    public static string GetGitHubToken()
    {
        if(SecretManage._github_token == String.Empty)
        {
            var path = Path.Combine( _base_path, "git_secret.txt");
            if(System.IO.File.Exists(path))
            {
                SecretManage._github_token = System.IO.File.ReadAllText(path).Trim();
            }
        }
        
        return SecretManage._github_token;
    }
}