using SportDJ.Commons.Models.Contents;

namespace SportDJ.Commons.Interfaces
{
    public interface IRegisterService
    {
        int GetCount(Track track);
        void Start(Track track);
        void Register(Track track);
    }
}