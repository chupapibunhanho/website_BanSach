using System.Configuration;

namespace webstie_BanSach.Models
{
    public partial class DatabaseDataContext
    {
        public DatabaseDataContext()
            : this(ConfigurationManager.ConnectionStrings["BookstoreConnectionString"].ConnectionString)
        {
            OnCreated();
        }
    }
}
