using System;
using System.Collections.Generic;
using System.Text;
using NBear.Common.Design;

namespace Temp
{
    public interface TempContractBase
    {
        string BaseID { get; set; }
    }

    public interface TempContract1 : TempContractBase
    {
        [PrimaryKey]
        Guid ID { get; set; }
        string C1 { get; set; }
        string BaseID { get; set; }
    }

    public interface TempPerson : Entity, TempContract1
    {
        string Name { get; set; }
    }

    public interface TempUser : TempPerson
    {
        string Email { get; set; }
    }

    public interface TempLocalUser : TempUser
    {
        string LoginID { get; set; }
    }

    [OutputNamespace("mtm3")]
    [MappingName("mtm3_User")]
    public interface User : Entity
    {
        [PrimaryKey]
        int ID { get; }
        string Name { get; set; }

        [FkQuery("UserID", Contained=true)]
        Phone[] Phones { get; set; }

        [ManyToManyQuery(typeof(UserGroup), Contained=true)]
        Group[] Groups { get; set; }
    }

    [OutputNamespace("mtm3")]
    [MappingName("mtm3_Phone")]
    public interface Phone : Entity
    {
        [PrimaryKey]
        int ID { get; }
        string Code { get; set; }

        int UserID { get; set; }
    }

    [OutputNamespace("mtm3")]
    [MappingName("mtm3_Group")]
    public interface Group : Entity
    {
        [PrimaryKey]
        int ID { get; }
        string Code { get; set; }
    }

    [OutputNamespace("mtm3")]
    [MappingName("mtm3_UserGroup")]
    [Relation]
    public interface UserGroup : Entity
    {
        [RelationKey(typeof(User))]
        int UserID { get; set; }
        [RelationKey(typeof(Group))]
        int GroupID { get; set; }
    }
}