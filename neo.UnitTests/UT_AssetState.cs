using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using Neo.Cryptography.ECC;
using Neo.IO;
using System.Globalization;
using System.IO;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_AssetState
    {
        AssetState uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new AssetState();
        }

        [TestMethod]
        public void AssetId_Get()
        {
            uut.AssetId.Should().BeNull();
        }

        [TestMethod]
        public void AssetId_Set()
        {
            UInt256 val = new UInt256();
            uut.AssetId = val;
            uut.AssetId.Should().Be(val);
        }

        [TestMethod]
        public void AssetType_Get()
        {
            uut.AssetType.Should().Be(AssetType.GoverningToken); // Uninitialised AssetType defaults to this enum value, be careful
        }

        [TestMethod]
        public void AssetType_Set()
        {
            AssetType val = new AssetType();
            uut.AssetType = val;
            uut.AssetType.Should().Be(val);
        }

        [TestMethod]
        public void Name_Get()
        {
            uut.Name.Should().BeNull();
        }

        [TestMethod]
        public void Name_Set()
        {
            string val = "wake up neo";
            uut.Name = val;
            uut.Name.Should().Be(val);
        }

        [TestMethod]
        public void Amount_Get()
        {
            uut.Amount.Should().Be(new Fixed8(0)); // defaults to 0
        }

        [TestMethod]
        public void Amount_Set()
        {
            Fixed8 val = new Fixed8();
            uut.Amount = val;
            uut.Amount.Should().Be(val);
        }

        [TestMethod]
        public void Available_Get()
        {
            uut.Available.Should().Be(new Fixed8(0)); // defaults to 0
        }

        [TestMethod]
        public void Available_Set()
        {
            Fixed8 val = new Fixed8();
            uut.Available = val;
            uut.Available.Should().Be(val);
        }

        [TestMethod]
        public void Precision_Get()
        {
            uut.Precision.Should().Be(0x00); // defaults to 0
        }

        [TestMethod]
        public void Precision_Set()
        {
            byte val = 0x42;
            uut.Precision = val;
            uut.Precision.Should().Be(val);
        }

        [TestMethod]
        public void FeeMode_Get()
        {
            AssetState.FeeMode.Should().Be(0);
        }

        [TestMethod]
        public void Fee_Get()
        {
            uut.Fee.Should().Be(new Fixed8(0)); // defaults to 0
        }

        [TestMethod]
        public void Fee_Set()
        {
            Fixed8 val = new Fixed8();
            uut.Fee = val;
            uut.Fee.Should().Be(val);
        }

        [TestMethod]
        public void FeeAddress_Get()
        {
            uut.FeeAddress.Should().BeNull();
        }

        [TestMethod]
        public void FeeAddress_Set()
        {
            UInt160 val = new UInt160();
            uut.FeeAddress = val;
            uut.FeeAddress.Should().Be(val);
        }

        [TestMethod]
        public void Owner_Get()
        {
            uut.Owner.Should().BeNull();
        }

        [TestMethod]
        public void Owner_Set()
        {
            ECPoint val = new ECPoint();
            uut.Owner = val;
            uut.Owner.Should().Be(val);
        }

        [TestMethod]
        public void Admin_Get()
        {
            uut.Admin.Should().BeNull();
        }

        [TestMethod]
        public void Admin_Set()
        {
            UInt160 val = new UInt160();
            uut.Admin = val;
            uut.Admin.Should().Be(val);
        }

        [TestMethod]
        public void Issuer_Get()
        {
            uut.Issuer.Should().BeNull();
        }

        [TestMethod]
        public void Issuer_Set()
        {
            UInt160 val = new UInt160();
            uut.Issuer = val;
            uut.Issuer.Should().Be(val);
        }

        [TestMethod]
        public void Expiration_Get()
        {
            uut.Expiration.Should().Be(0u);
        }

        [TestMethod]
        public void Expiration_Set()
        {
            uint val = 42;
            uut.Expiration = val;
            uut.Expiration.Should().Be(val);
        }

        [TestMethod]
        public void IsFrozen_Get()
        {
            uut.IsFrozen.Should().Be(false);
        }

        [TestMethod]
        public void IsFrozen_Set()
        {
            uut.IsFrozen = true;
            uut.IsFrozen.Should().Be(true);
        }

        private void setupAssetStateWithValues(AssetState assetState, out UInt256 assetId, out AssetType assetType, out string name, out Fixed8 amount, out Fixed8 available, out byte precision, out Fixed8 fee, out UInt160 feeAddress, out ECPoint owner, out UInt160 admin, out UInt160 issuer, out uint expiration, out bool isFrozen)
        {
            assetId = new UInt256(TestUtils.GetByteArray(32, 0x20));
            assetState.AssetId = assetId;
            assetType = AssetType.Token;
            assetState.AssetType = assetType;

            name = "neo";
            assetState.Name = name;

            amount = new Fixed8(42);
            assetState.Amount = amount;

            available = new Fixed8(43);
            assetState.Available = available;

            precision = 0x42;
            assetState.Precision = precision;

            fee = new Fixed8(44);
            assetState.Fee = fee;

            feeAddress = new UInt160(TestUtils.GetByteArray(20, 0x21));
            assetState.FeeAddress = feeAddress;

            owner = ECPoint.DecodePoint(TestUtils.GetByteArray(1,0x00), ECCurve.Secp256r1);
            assetState.Owner = owner;

            admin = new UInt160(TestUtils.GetByteArray(20, 0x22));
            assetState.Admin = admin;

            issuer = new UInt160(TestUtils.GetByteArray(20, 0x23));
            assetState.Issuer = issuer;

            expiration = 42u;
            assetState.Expiration = expiration;

            isFrozen = true;
            assetState.IsFrozen = isFrozen;
        }

        [TestMethod]
        public void Size_Get_Default()
        {
            UInt256 assetId;
            AssetType assetType;
            string name;
            Fixed8 amount, available, fee;
            byte precision;
            UInt160 feeAddress, admin, issuer;
            ECPoint owner;
            uint expiration;
            bool isFrozen;
            setupAssetStateWithValues(uut, out assetId, out assetType, out name, out amount, out available, out precision, out fee, out feeAddress, out owner, out admin, out issuer, out expiration, out isFrozen);

            uut.Size.Should().Be(130); // 1 + 32 + 1 + 4 + 8 + 8 + 1 + 1 + 8 + 20 + 1 + 20 + 20 + 4 + 1
        }

        [TestMethod]
        public void Clone()
        {
            UInt256 assetId;
            AssetType assetType;
            string name;
            Fixed8 amount, available, fee;
            byte precision;
            UInt160 feeAddress, admin, issuer;
            ECPoint owner;
            uint expiration;
            bool isFrozen;
            setupAssetStateWithValues(uut, out assetId, out assetType, out name, out amount, out available, out precision, out fee, out feeAddress, out owner, out admin, out issuer, out expiration, out isFrozen);

            AssetState newAs = ((ICloneable<AssetState>)uut).Clone();

            newAs.AssetId.Should().Be(assetId);
            newAs.AssetType.Should().Be(assetType);
            newAs.Name.Should().Be(name);
            newAs.Amount.Should().Be(amount);
            newAs.Available.Should().Be(available);
            newAs.Precision.Should().Be(precision);
            newAs.Fee.Should().Be(fee);
            newAs.FeeAddress.Should().Be(feeAddress);
            newAs.Owner.Should().Be(owner);
            newAs.Admin.Should().Be(admin);
            newAs.Issuer.Should().Be(issuer);
            newAs.Expiration.Should().Be(expiration);
            newAs.IsFrozen.Should().Be(isFrozen);
        }

        [TestMethod]
        public void FromReplica()
        {
            AssetState assetState = new AssetState();
            UInt256 assetId;
            AssetType assetType;
            string name;
            Fixed8 amount, available, fee;
            byte precision;
            UInt160 feeAddress, admin, issuer;
            ECPoint owner;
            uint expiration;
            bool isFrozen;
            setupAssetStateWithValues(assetState, out assetId, out assetType, out name, out amount, out available, out precision, out fee, out feeAddress, out owner, out admin, out issuer, out expiration, out isFrozen);


            ((ICloneable<AssetState>)uut).FromReplica(assetState);
            uut.AssetId.Should().Be(assetId);
            uut.AssetType.Should().Be(assetType);
            uut.Name.Should().Be(name);
            uut.Amount.Should().Be(amount);
            uut.Available.Should().Be(available);
            uut.Precision.Should().Be(precision);
            uut.Fee.Should().Be(fee);
            uut.FeeAddress.Should().Be(feeAddress);
            uut.Owner.Should().Be(owner);
            uut.Admin.Should().Be(admin);
            uut.Issuer.Should().Be(issuer);
            uut.Expiration.Should().Be(expiration);
            uut.IsFrozen.Should().Be(isFrozen);
        }

        [TestMethod]
        public void Deserialize()
        {
            UInt256 assetId;
            AssetType assetType;
            string name;
            Fixed8 amount, available, fee;
            byte precision;
            UInt160 feeAddress, admin, issuer;
            ECPoint owner;
            uint expiration;
            bool isFrozen;
            setupAssetStateWithValues(new AssetState(), out assetId, out assetType, out name, out amount, out available, out precision, out fee, out feeAddress, out owner, out admin, out issuer, out expiration, out isFrozen);

            byte[] data = new byte[] { 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 96, 3, 110, 101, 111, 42, 0, 0, 0, 0, 0, 0, 0, 43, 0, 0, 0, 0, 0, 0, 0, 66, 0, 44, 0, 0, 0, 0, 0, 0, 0, 33, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 0, 34, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 35, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 42, 0, 0, 0, 1 };
            int index = 0;
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    uut.Deserialize(reader);
                }
            }

            uut.AssetId.Should().Be(assetId);
            uut.AssetType.Should().Be(assetType);
            uut.Name.Should().Be(name);
            uut.Amount.Should().Be(amount);
            uut.Available.Should().Be(available);
            uut.Precision.Should().Be(precision);
            uut.Fee.Should().Be(fee);
            uut.FeeAddress.Should().Be(feeAddress);
            uut.Owner.Should().Be(owner);
            uut.Admin.Should().Be(admin);
            uut.Issuer.Should().Be(issuer);
            uut.Expiration.Should().Be(expiration);
            uut.IsFrozen.Should().Be(isFrozen);
        }


        [TestMethod]
        public void Serialize()
        {
            UInt256 assetId;
            AssetType assetType;
            string name;
            Fixed8 amount, available, fee;
            byte precision;
            UInt160 feeAddress, admin, issuer;
            ECPoint owner;
            uint expiration;
            bool isFrozen;
            setupAssetStateWithValues(uut, out assetId, out assetType, out name, out amount, out available, out precision, out fee, out feeAddress, out owner, out admin, out issuer, out expiration, out isFrozen);

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    uut.Serialize(writer);
                    data = stream.ToArray();
                }
            }

            byte[] requiredData = new byte[] { 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 96, 3, 110, 101, 111, 42, 0, 0, 0, 0, 0, 0, 0, 43, 0, 0, 0, 0, 0, 0, 0, 66, 0, 44, 0, 0, 0, 0, 0, 0, 0, 33, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 0, 34, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 35, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 42, 0, 0, 0, 1 };
            data.Length.Should().Be(130);
            for (int i = 0; i < 130; i++)
            {
                data[i].Should().Be(requiredData[i]);
            }
        }

        [TestMethod]
        public void GetName()
        {
            UInt256 assetId;
            AssetType assetType;
            string name;
            Fixed8 amount, available, fee;
            byte precision;
            UInt160 feeAddress, admin, issuer;
            ECPoint owner;
            uint expiration;
            bool isFrozen;
            setupAssetStateWithValues(uut, out assetId, out assetType, out name, out amount, out available, out precision, out fee, out feeAddress, out owner, out admin, out issuer, out expiration, out isFrozen);

            uut.GetName().Should().Be("neo");
            // The base class GetName() method should be be optimised to avoid the slow try / catch
        }

        [TestMethod]
        public void GetName_Culture()
        {
            UInt256 assetId;
            AssetType assetType;
            string name;
            Fixed8 amount, available, fee;
            byte precision;
            UInt160 feeAddress, admin, issuer;
            ECPoint owner;
            uint expiration;
            bool isFrozen;
            setupAssetStateWithValues(uut, out assetId, out assetType, out name, out amount, out available, out precision, out fee, out feeAddress, out owner, out admin, out issuer, out expiration, out isFrozen);
            uut.Name = "[{\"lang\":\"zh-CN\",\"name\":\"小蚁股\"},{\"lang\":\"en\",\"name\":\"Neo\"}]";

            uut.GetName(new CultureInfo("zh-CN")).Should().Be("小蚁股");
            uut.GetName(new CultureInfo("en")).Should().Be("Neo");
        }

        [TestMethod]
        public void GetName_Culture_En()
        {
            UInt256 assetId;
            AssetType assetType;
            string name;
            Fixed8 amount, available, fee;
            byte precision;
            UInt160 feeAddress, admin, issuer;
            ECPoint owner;
            uint expiration;
            bool isFrozen;
            setupAssetStateWithValues(uut, out assetId, out assetType, out name, out amount, out available, out precision, out fee, out feeAddress, out owner, out admin, out issuer, out expiration, out isFrozen);
            uut.Name = "[{\"lang\":\"zh-CN\",\"name\":\"小蚁股\"},{\"lang\":\"en\",\"name\":\"Neo\"}]";

            CultureInfo.CurrentCulture = new CultureInfo("en");
            uut.GetName().Should().Be("Neo");
        }

        [TestMethod]
        public void GetName_Culture_Cn()
        {
            UInt256 assetId;
            AssetType assetType;
            string name;
            Fixed8 amount, available, fee;
            byte precision;
            UInt160 feeAddress, admin, issuer;
            ECPoint owner;
            uint expiration;
            bool isFrozen;
            setupAssetStateWithValues(uut, out assetId, out assetType, out name, out amount, out available, out precision, out fee, out feeAddress, out owner, out admin, out issuer, out expiration, out isFrozen);
            uut.Name = "[{\"lang\":\"zh-CN\",\"name\":\"小蚁股\"},{\"lang\":\"en\",\"name\":\"Neo\"}]";

            CultureInfo.CurrentCulture = new CultureInfo("zh-CN");
            uut.GetName().Should().Be("小蚁股");
        }

        [TestMethod]
        public void GetName_Culture_Unknown()
        {
            UInt256 assetId;
            AssetType assetType;
            string name;
            Fixed8 amount, available, fee;
            byte precision;
            UInt160 feeAddress, admin, issuer;
            ECPoint owner;
            uint expiration;
            bool isFrozen;
            setupAssetStateWithValues(uut, out assetId, out assetType, out name, out amount, out available, out precision, out fee, out feeAddress, out owner, out admin, out issuer, out expiration, out isFrozen);
            uut.Name = "[{\"lang\":\"zh-CN\",\"name\":\"小蚁股\"},{\"lang\":\"en\",\"name\":\"Neo\"}]";

            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            uut.GetName().Should().Be("Neo"); // defaults to english IF english is in the name
        }

        [TestMethod]
        public void GetName_Culture_Unknown_NoEn()
        {
            UInt256 assetId;
            AssetType assetType;
            string name;
            Fixed8 amount, available, fee;
            byte precision;
            UInt160 feeAddress, admin, issuer;
            ECPoint owner;
            uint expiration;
            bool isFrozen;
            setupAssetStateWithValues(uut, out assetId, out assetType, out name, out amount, out available, out precision, out fee, out feeAddress, out owner, out admin, out issuer, out expiration, out isFrozen);
            uut.Name = "[{\"lang\":\"zh-CN\",\"name\":\"小蚁股\"},{\"lang\":\"foo\",\"name\":\"bar\"}]";

            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            uut.GetName().Should().Be("小蚁股"); // defaults to first name IF english is not in the name
        }


    }
}
