//why use apimarker?
//to the assembly/application represented by this type
//It's just a marker type used by the integration test to identify the API host
//couldve used partial class but it might risk the project sooo
namespace Auth.API;

public sealed class ApiMarker
{
}