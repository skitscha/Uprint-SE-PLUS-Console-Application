using CartridgeWriter;


namespace Uprint_SE_PLUS
{
    class Program
    {
        private static byte[] eepromUID = new byte[8] { 0x0D5, 0x010, 0x014, 0x0D0, 0x016, 0x0AD, 0x0E7, 0x0B3 };
        private static byte[] encrypted = new byte[128] { 
            0x043,0x0FF,0x055,0x06F,0x0E3,0x0AA,
            0x07D,0x057,0x078,0x0F0,0x069,0x078,
            0x066,0x056,0x0B0,0x09B,0x018,0x0D5,
            0x080,0x0BC,0x0FE,0x084,0x04E,0x0A2,
            0x023,0x0C5,0x0E3,0x084,0x0AF,0x0E6,
            0x0DC,0x0F1,0x08E,0x010,0x047,0x0E5,
            0x0E0,0x0EA,0x05E,0x0DD,0x0E0,0x02A,
            0x0E1,0x003,0x043,0x00A,0x0B4,0x023,
            0x0E0,0x02A,0x0E1,0x003,0x043,0x00A,
            0x0B4,0x023,0x0D0,0x0EA,0x073,0x0DF,
            0x0D0,0x0BA,0x05D,0x0D8,0x0D9,0x07F,
            0x000,0x000,0x000,0x000,0x02E,0x02F,
            0x055,0x0AA,0x055,0x02C,0x0EF,0x06B,
            0x043,0x0BE,0x0A6,0x082,0x000,0x000,
            0x000,0x000,0x000,0x000,0x0D5,0x0D9,
            0x0E0,0x062,0x0C3,0x066,0x038,0x061,
            0x064,0x075,0x0E1,0x09E,0x001,0x000,
            0x000,0x000,0x053,0x054,0x052,0x041,
            0x054,0x041,0x053,0x059,0x053,0x0DA,
            0x0F5,0x0E0,0x016,0x067,0x0CA,0x089,
            0x0A7,0x069,0x05E,0x011,0x0BB,0x048,
            0x0E1,0x0A1
        };
        static void Main(string[] args)
        {
        Cartridge c = null;
        Machine machine = Machine.FromType("Uprint SE");


        c = new Cartridge(encrypted, machine, eepromUID);
        }


        //Sets the text to the textboxes
        //private void LoadControls() 
        //{
        //    //txtEEPROMUID.Text = clear_ID(input_flash);
        //    txtEEPROMUID.Text = c.EEPROMUID.Reverse().HexString();
        //    txtKeyFragment.Text = c.KeyFragment;
        //    txtSerialNumberCurrent.Text = c.SerialNumber.ToString("f1");
        //    txtSerialNumberChangeTo.Text = txtSerialNumberCurrent.Text;
        //    cboMaterialCurrent.Text = c.Material.Name;
        //    cboMaterialChangeTo.Text = cboMaterialCurrent.Text;
        //    txtManufacturingLotCurrent.Text = c.ManufacturingLot;
        //    txtManufacturingLotChangeTo.Text = txtManufacturingLotCurrent.Text;
        //    txtManufacturingDateCurrent.Text = c.ManfuacturingDate.ToString("dd'-'MM'-'yyyy - HH':'mm':'ss");
        //    txtManufacturingDateChangeTo.Text = txtManufacturingDateCurrent.Text;
        //    txtLastUseDateCurrent.Text = c.UseDate.ToString("dd'-'MM'-'yyyy - HH':'mm':'ss");
        //    txtLastUseDateChangeTo.Text = txtLastUseDateCurrent.Text;
        //    txtInitialQuantityCurrent.Text = c.InitialMaterialQuantity.ToString();
        //    txtInitialQuantityChangeTo.Text = txtInitialQuantityCurrent.Text;
        //    txtCurrentQuantityCurrent.Text = c.CurrentMaterialQuantity.ToString();
        //    txtCurrentQuantityChangeTo.Text = c.InitialMaterialQuantity.ToString();
        //    txtVersionCurrent.Text = c.Version.ToString();
        //    txtVersionChangeTo.Text = txtVersionCurrent.Text;
        //    txtSignatureCurrent.Text = c.Signature;
        //    txtSignatureChangeTo.Text = txtSignatureCurrent.Text;

        //    //Random zufall = new Random();
        //    //txtSerialNumberChangeTo.Text = zufall.Next(10000000, 99999999).ToString() + ",0";

        //}
    }
}
