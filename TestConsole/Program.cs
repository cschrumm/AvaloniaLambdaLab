// See https://aka.ms/new-console-template for more information



using System.Text.Json;
using System.Xml.Linq;
using Service.Library;

/*
 * This will act as a scratch pad for testing out this code...
 */

Console.WriteLine("Hello, World!");

var pp = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);


var lmbdaKey = System.Environment.GetEnvironmentVariable("LAMBDA_KEY");

var cclient = new LambdaCloudClient(lmbdaKey ?? "");

/*
 *
 * 1. List instances
 * 2. Create instance
 * 3. Figure out ssh to connect and then run git commands.
 */

System.Threading.Thread.Sleep(1000);
var mncl = new MainGuiBackend();

mncl.OnLogMessage += (msg) => 
{
    Console.WriteLine($"SSH Log: {msg}");
};
/*

var inst = await mncl.InStanceTypes();
var kys = await mncl.ListSshKeys();
var fss = await mncl.ListFileSystems();


var to_st = inst.Where(k=> k.Name.IndexOf("gh200") > 0).FirstOrDefault();

var ids = await mncl.CreateServer("Test-Instance-1", to_st?.Name, to_st?.Region?.Name, ky.Name,null);

  var inst = await mncl.InStanceTypes();
   var kys = await mncl.ListSshKeys();
   var fss = await mncl.ListFileSystems();
   var imgs = await mncl.ListImages();
   
   var to_st = inst.Where(k=> k.Name.IndexOf("gh200") > 0).FirstOrDefault();
   
   var ky = kys.Where(k => k.Name.IndexOf("max") > 0).FirstOrDefault();
   
   var cmp = mncl.CompantibleImages(to_st, imgs);
   
   var lst_im = cmp[^1];
   
   var ids = await mncl.CreateServer("Test-Instance-1", to_st?.Name, to_st?.Region?.Name, ky.Name,lst_im.Id,null);
   
   
   Console.WriteLine("good");

var instances = await mncl.ListInstances();
   
   var kypath = "/home/madmax-machine/.ssh/madmax-machine-2.pem";
   mncl.SShSetup(instances[0], kypath, (new Guid()).ToString());
   
*/

var instances = await mncl.ListInstances();
   
var kypath = "/home/madmax-machine/.ssh/madmax-machine-2.pem";

var gd = Guid.NewGuid().ToString();
mncl.SShSetup(instances[0], kypath, gd);


/*


var instances = await mncl.ListInstances();
   
var kypath = "/home/madmax-machine/.ssh/madmax-machine-2.pem";
mncl.SShSetup(instances[0], kypath, (new Guid()).ToString());

*/ 
Console.WriteLine("good");


//Console.WriteLine("Instance Types:");


//var instances = await mncl.ListInstances();

//var kypath = "/home/madmax-machine/.ssh/madmax-machine-2.pem";
//mncl.SShSetup(instances[0], kypath);


//Console.WriteLine("Instance Types:");
/*




var ssh = new SshClientManager();
   

   
var exists = System.IO.File.Exists(kypath);
   
var dr = System.IO.Directory.Exists("/home/madmax-machine/.ssh");
   
ssh.ConnectWithPrivateKey("192.222.59.234", 22, "ubuntu", kypath);
var rslt = ssh.ExecuteCommand("ls -la");
*/



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
