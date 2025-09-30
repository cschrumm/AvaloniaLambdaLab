// See https://aka.ms/new-console-template for more information



using System.Text.Json;
using System.Xml.Linq;
using Service.Library;

/*
 * This will act as a scratch pad for testing out this code...
 */

Console.WriteLine("Hello, World!");

var pp = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);




/*
 *
 * 1. List instances
 * 2. Create instance
 * 3. Figure out ssh to connect and then run git commands.
 */

System.Threading.Thread.Sleep(1000);

var lmbdaKey = System.Environment.GetEnvironmentVariable("LAMBDA_KEY");

var cclient = new LambdaCloudClient(lmbdaKey ?? "");


var ssh = new SshClientManager();
   
var kypath = "/home/madmax-machine/.ssh/madmax-machine-2.pem";
   
var exists = System.IO.File.Exists(kypath);
   
var dr = System.IO.Directory.Exists("/home/madmax-machine/.ssh");
   
ssh.ConnectWithPrivateKey("192.222.59.234", 22, "ubuntu", kypath);
var rslt = ssh.ExecuteCommand("ls -la");



/*
 
 var ssh = new SshClientManager();
   
   var kypath = "/home/madmax-machine/.ssh/madmax-machine-2.pem";
   
   var exists = System.IO.File.Exists(kypath);
   
   var dr = System.IO.Directory.Exists("/home/madmax-machine/.ssh");
   
   ssh.ConnectWithPrivateKey("192.222.59.234", 22, "ubuntu@192.222.59.234", kypath);
   var rslt = ssh.ExecuteCommand("ls -la");
   
var instances = await cclient.ListInstanceTypesAsync();

foreach (var item in instances)
{
    Console.WriteLine($"Instance Type: {item.Key}");
    Console.WriteLine($"Instance Type: {item.Value}");
}
*/

/*
 *data '{
     "region_name": "europe-central-1",
     "instance_type_name": "gpu_8x_a100",
     "ssh_key_names": [
       "my-public-key"
     ],
     "file_system_names": [
       "my-filesystem"
     ],
     "file_system_mounts": [
       {
         "mount_point": "/data/custom-mount-point",
         "file_system_id": "398578a2336b49079e74043f0bd2cfe8"
       }
     ],
     "hostname": "headnode1",
     "name": "My Instance",
     "image": {
       "id": "string"
     },
     "user_data": "string",
     "tags": [
       {
         "key": "key1",
         "value": "value1"
       }
     ],
     "firewall_rulesets": [
       {
         "id": "c4d291f47f9d436fa39f58493ce3b50d"
       }
     ]
   }'
 * IHatMailP@ssw0rds$%
 */
/*
var prv_key = "/home/madmax-machine/.ssh/id_rsa_lambda";

var instance = new InstanceLaunchRequest();
instance.RegionName = "europe-central-1";
instance.InstanceTypeName = "europe-central-1";
instance.SshKeyNames = new List<string>() { "my-public-key" };
instance.FileSystemNames = new List<string>();
instance.Name = "My Instance";

var nm =await cclient.LaunchInstanceAsync(instance);

Console.WriteLine($"Launched Instance: {nm.InstanceIds[0]}");



*/

var instnc = await cclient.ListInstancesAsync();

foreach (var item in instnc)
{
  Console.WriteLine($"Instance: {item.Name} ID: {item.Id} State: {item.Status}");
}
