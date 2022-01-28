using Chemistry;
using IO.MzML;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using MzLibUtil;
using NUnit.Framework;
using Proteomics;
using Proteomics.ProteolyticDigestion;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Test
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public sealed class TestDeconvolution
    {
        [Test]
        [TestCase(586.2143122,24, 41983672, 586.2)]//This is a lesser abundant charge state envelope at the low mz end
        [TestCase(740.372202090153, 19, 108419280, 740.37)]//This is the most abundant charge state envelope
        [TestCase(1081.385183, 13, 35454636, 1081.385)]//This is a lesser abundant charge state envelope at the high mz end
        public void TestDeconvolutionProteoformMultiChargeState(double selectedIonMz, int selectedIonChargeStateGuess, double selectedIonIntensity, double isolationMz)
        {
            MsDataScan[] Scans = new MsDataScan[1];

            //txt file, not mgf, because it's an MS1. Most intense proteoform has mass of ~14037.9 Da
            string Ms1SpectrumPath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"DataFiles\14kDaProteoformMzIntensityMs1.txt");

            string[] spectrumLines = File.ReadAllLines(Ms1SpectrumPath);

            int mzIntensityPairsCount = spectrumLines.Length;
            double[] ms1mzs = new double[mzIntensityPairsCount];
            double[] ms1intensities = new double[mzIntensityPairsCount];

            for (int i = 0; i < mzIntensityPairsCount; i++)
            {
                string[] pair = spectrumLines[i].Split('\t');
                ms1mzs[i] = Convert.ToDouble(pair[0], CultureInfo.InvariantCulture);
                ms1intensities[i] = Convert.ToDouble(pair[1], CultureInfo.InvariantCulture);
            }

            MzSpectrum spectrum = new MzSpectrum(ms1mzs, ms1intensities, false);

            Scans[0] = new MsDataScan(spectrum, 1, 1, false, Polarity.Positive, 1.0, new MzRange(495, 1617), "first spectrum", MZAnalyzerType.Unknown, spectrum.SumOfAllY, null, null, null, selectedIonMz, selectedIonChargeStateGuess, selectedIonIntensity, isolationMz, 4);

            var myMsDataFile = new FakeMsDataFile(Scans);

            MsDataScan scan = myMsDataFile.GetAllScansList()[0];

            List<IsotopicEnvelope> isolatedMasses = scan.GetIsolatedMassesAndCharges(spectrum, 1, 60, 4, 3).ToList();

            List<double> monoIsotopicMasses = isolatedMasses.Select(m => m.MonoisotopicMass).ToList();

            //The primary monoisotopic mass should be the same regardless of which peak in which charge state was selected for isolation.
            //this case is interesting because other monoisotopic mass may have a sodium adduct. The unit test could be expanded to consider this.
            Assert.That(monoIsotopicMasses[0], Is.EqualTo(14037.926829).Within(.0005));
        }

        [Test]
        public static void CheckGetMostAbundantObservedIsotopicMass()
        {
            /*
            string fullFilePathWithExtension = @"D:\TDBU\Jurkat\TD-Projects-JurkatTopDownSeanDaiPaper\FXN11_tr1_032017.raw";
           
            ThermoRawFileReader staticRaw = ThermoRawFileReader.LoadAllStaticData(fullFilePathWithExtension);
            List<MsDataScan> scan = staticRaw.GetAllScansList();
            scan = scan.Where(p => p.MsnOrder == 1).ToList();
            scan = scan.Where(p => p.OneBasedScanNumber == 587).ToList(); //pull out single scan

            MzSpectrum spec = scan[0].MassSpectrum;
            MzRange theRange = new MzRange(spec.XArray.Min(), spec.XArray.Max());
            int minAssumedChargeState = 1;
            int maxAssumedChargeState = 60;
            double deconvolutionTolerancePpm = 20;
            double intensityRatioLimit = 3;

            List<IsotopicEnvelope> lie = spec.Deconvolute(theRange, minAssumedChargeState, maxAssumedChargeState, deconvolutionTolerancePpm, intensityRatioLimit).ToList();

            
            //check all most abundant isotopic masses >= the monoisotopic masses
            lie = lie.Where(p => p.Charge > 2).ToList(); //need to remove charge < 3... ? 
            var mostabundantmasses = lie.Select(p => p.MostAbundantObservedIsotopicMass).ToList();
            var masses = lie.Select(p => p.MonoisotopicMass).ToList();
            for (int i=0; i < masses.Count; i++)
            {
                Assert.GreaterOrEqual(mostabundantmasses[i], masses[i]);
            }
            */

            //PKRKAEGDAKGDKAKVKDEPQRRSARLSAKPAPPKPEPKPKKAPAKKGEKVPKGKKGKADAGKEGNNPAENGDAKTDQAQKAEGAGDAK
            string singleScan = Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", "FXN11_tr1_032017_scan721.mzML");
            //string singleScan = @"C:\Users\KAP\source\repos\kyp4\mzLib\Test\DataFiles\05-13-16_cali_MS_60K-res_MS.raw";
            Mzml singleMZML = Mzml.LoadAllStaticData(singleScan);

            List<MsDataScan> singlescan = singleMZML.GetAllScansList();
            
            MzSpectrum singlespec = singlescan[0].MassSpectrum;
            MzRange singleRange = new MzRange(singlespec.XArray.Min(), singlespec.XArray.Max());
            int minAssumedChargeState = 1;
            int maxAssumedChargeState = 60;
            double deconvolutionTolerancePpm = 20;
            double intensityRatioLimit = 3;

            List<IsotopicEnvelope> lie2 = singlespec.Deconvolute(singleRange, minAssumedChargeState, maxAssumedChargeState, deconvolutionTolerancePpm, intensityRatioLimit).ToList();

            List<IsotopicEnvelope>  lie2_charge12 = lie2.Where(p => p.Charge == 12).ToList();
            Assert.That(lie2_charge12[0].MostAbundantObservedIsotopicMass, Is.EqualTo(772.75984 * 12).Within(0.5));

            List<IsotopicEnvelope> lie2_charge15 = lie2.Where(p => p.Charge == 15).ToList();
            Assert.That(lie2_charge15[0].MostAbundantObservedIsotopicMass, Is.EqualTo(618.40933 * 15).Within(0.5));

        }
    }
}