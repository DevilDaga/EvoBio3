﻿using System.Collections.Generic;
using System.Linq;
using EvoBio3.Collections;
using EvoBio3.Core;
using EvoBio3.Core.Enums;
using EvoBio3.Core.Extensions;
using EvoBio3.Core.Interfaces;
using MathNet.Numerics.Statistics;
using MoreLinq;

namespace EvoBio3.Versions
{
	public class SingleIterationBaseVersion :
		SingleIterationBase<Individual, IndividualGroup, Population, Variables>
	{
		public SingleIterationBaseVersion ( )
		{
		}

		public SingleIterationBaseVersion (
			Variables v,
			bool isLoggingEnabled ) :
			base ( v, null, isLoggingEnabled )
		{
		}

		public SingleIterationBaseVersion (
			Variables v,
			IAdjustmentRules<Individual, IndividualGroup, Variables,
				ISingleIteration<Individual, IndividualGroup, Variables>> adjustmentRules,
			bool isLoggingEnabled ) :
			base ( v, adjustmentRules, isLoggingEnabled )
		{
		}

		public override void Perish1 ( )
		{
			AdjustmentRules.AdjustStep1 ( );

			Step1PerishCount = Utility.NextGaussianIntInRange ( V.MeanPerishStep1, V.SdPerishStep1,
			                                                    0, V.PopulationSize - 1 );

			var survivorsCount = AllIndividuals.Count - Step1PerishCount;

			( Step1Survivors, Step1Rejects ) = AllIndividuals.ChooseBy ( survivorsCount, x => x.PhenotypicQuality );
			foreach ( var ind in Step1Rejects )
				ind.Perish ( );

			if ( IsLoggingEnabled )
			{
				Logger.Debug ( "\n\nPerish 1:\n" );
				Logger.Debug ( $"Amount to perish = {Step1PerishCount}" );
				Logger.Debug ( "Perished Individuals:" );
				Logger.Debug ( Step1Rejects
					               .OrderBy ( x => x.Type )
					               .ThenBy ( x => x.Id )
					               .ToTable ( x => new
						               {
							               _Type = x.Type,
							               x.Id,
							               Qp = $"{x.PhenotypicQuality:F4}"
						               }
					               )
				);
			}
		}

		public override void Perish2 ( )
		{
			if ( IsLoggingEnabled )
				Logger.Debug ( "\n\nPerish 2:\n" );

			if ( Step1PerishCount >= V.PopulationSize )
				return;

			AdjustmentRules.AdjustStep2 ( );

			Step2PerishCount = Utility.NextGaussianIntInRange ( V.MeanPerishStep2, V.SdPerishStep2,
			                                                    0, Step1Survivors.Count - 1 );

			var step2SurvivorsCount = Step1Survivors.Count - Step2PerishCount;

			( Step2Survivors, Step2Rejects ) = Step1Survivors.ChooseBy ( step2SurvivorsCount, x => x.S );
			foreach ( var ind in Step2Rejects )
				ind.Perish ( );

			if ( IsLoggingEnabled )
			{
				Logger.Debug ( "\n" );
				Logger.Debug ( $"Amount to perish = {Step2PerishCount}" );
				Logger.Debug ( "Perished Individuals:" );
				Logger.Debug ( Step2Rejects
					               .OrderBy ( x => x.Type )
					               .ThenBy ( x => x.Id )
					               .ToTable ( x => new
						               {
							               _Type = x.Type,
							               x.Id,
							               Qp = $"{x.PhenotypicQuality:F4}",
							               S  = $"{x.S:F4}"
						               }
					               )
				);
			}
		}

		public override void CalculateFecundity ( )
		{
			if ( IsLoggingEnabled )
				Logger.Debug ( "\n\nCalculate Fecundity:\n" );

			AdjustmentRules.CalculateFecundity ( );
			foreach ( var group in AllGroups )
			{
				group.CalculateTotalFecundity ( );
				group.CalculateLostFecundity ( );
			}
		}

		public override void CalculateAdjustedFecundity ( )
		{
			var lostFecunditySum = AllGroups.Sum ( x => x.LostFecundity );
			var totalFecunditySum = AllGroups.Sum ( x => x.TotalFecundity );
			foreach ( var group in AllGroups.Where ( x => x.TotalFecundity != 0 ) )
			{
				var term2 = ( 1d - V.R ) * V.Y * lostFecunditySum / totalFecunditySum;
				var term1 = V.R * V.Y * group.LostFecundity / group.TotalFecundity;
				var multiplier = 1d + term1 + term2;

				foreach ( var individual in group )
					individual.AdjustedFecundity = individual.Fecundity * multiplier;
			}

			if ( IsLoggingEnabled )
			{
				Logger.Debug ( "\n\nCalculate Adjusted Fecundity:\n" );
				foreach ( var group in AllGroups )
					Logger.Debug ( group.ToTable (
						               x => new
						               {
							               _1_Id                = x.Id,
							               _2_Qp                = $"{x.PhenotypicQuality:F4}",
							               _3_Fecundity         = $"{x.Fecundity:F4}",
							               _4_AdjustedFecundity = $"{x.AdjustedFecundity:F4}"
						               }
					               )
					);
			}
		}

		public override List<Individual> GetParents ( ) =>
			Population.RepetitiveChooseBy ( V.PopulationSize, x => x.AdjustedFecundity );

		protected override void Reproduce ( List<Individual> parents,
		                                    List<Individual> offsprings,
		                                    Dictionary<IndividualType, int> lastId )
		{
			var totalGenetic = parents.Sum ( x => x.GeneticQuality );
			foreach ( var parent in parents )
			{
				var z = parent.GeneticQuality * V.PopulationSize * 10 / totalGenetic;

				var geneticQuality = Utility.NextGaussianNonNegative ( z, V.SdQuality );

				var offspring = parent.Reproduce (
					++lastId[parent.Type],
					geneticQuality,
					Utility.NextGaussianNonNegative ( geneticQuality, V.SdPheno )
				);

				offsprings.Add ( offspring );
				History.Add ( ( parent, offspring ) );
			}

			if ( IsLoggingEnabled )
			{
				Logger.Debug ( "\n\nReproduce:\n" );
				var table = Enumerable.TakeLast ( History, V.PopulationSize )
					.OrderBy ( x => x.parent.Type )
					.ThenBy ( x => x.parent.Id )
					.ToTable ( x => new
						{
							_1_Parent    = x.parent.Name,
							_2_Offspring = x.offspring.Name,
							_3_Qg        = $"{x.offspring.GeneticQuality:F4}",
							_4_Qp        = $"{x.offspring.PhenotypicQuality:F4}"
						}
					);
				Logger.Debug ( table );
			}
		}

		public override void CalculateHeritability ( )
		{
			if ( GenerationsPassed <= 2 )
				return;

			var groups = History
				.GroupBy ( x => x.parent )
				.Select ( x => ( parent: x.Key, offsprings: x.Select ( y => y.offspring ).ToList ( ) ) )
				.Where ( x => x.offsprings.Any ( ) )
				.ToList ( );

			var covPhenoQuality = groups.Select ( x => x.parent.PhenotypicQuality )
				.PopulationCovariance ( groups.Select ( x => x.offsprings.Average ( y => y.PhenotypicQuality ) ) );
			var covGeneticQuality = groups.Select ( x => x.parent.GeneticQuality )
				.PopulationCovariance ( groups.Select ( x => x.offsprings.Average ( y => y.GeneticQuality ) ) );

			var varPhenoQuality = groups
				.Select ( x => x.parent.PhenotypicQuality )
				.PopulationVariance ( );
			var varGeneticQuality = groups
				.Select ( x => x.parent.GeneticQuality )
				.PopulationVariance ( );

			var pairs = History.Take ( History.Count - V.PopulationSize );
			groups = pairs
				.GroupBy ( x => x.parent )
				.Select ( x => ( parent: x.Key, offsprings: x.Select ( y => y.offspring ).ToList ( ) ) )
				.Where ( x => x.offsprings.Any ( ) )
				.ToList ( );
			var covReproduction = groups.Select ( x => (double) x.parent.OffspringCount )
				.PopulationCovariance ( groups.Select ( x => x.offsprings.Average ( y => y.OffspringCount ) ) );
			var varReproduction = groups
				.Select ( x => x.parent.PhenotypicQuality )
				.PopulationVariance ( );

			Heritability = new HeritabilitySummary
			{
				PhenotypicQuality           = covPhenoQuality / varPhenoQuality,
				VariancePhenotypicQuality   = varPhenoQuality,
				CovariancePhenotypicQuality = covPhenoQuality,
				GeneticQuality              = covGeneticQuality / varGeneticQuality,
				VarianceGeneticQuality      = varGeneticQuality,
				CovarianceGeneticQuality    = covGeneticQuality,
				Reproduction                = covReproduction / varReproduction,
				VarianceReproduction        = varReproduction,
				CovarianceReproduction      = covReproduction
			};

			if ( IsLoggingEnabled )
			{
				Logger.Debug ( "\n\nHeritability Calculations: \n" );
				var generations = History
					.GroupBy ( x => x.parent )
					.Select ( x => ( parent: x.Key,
					                 offsprings: x.Select ( y => y.offspring )
						                 .ToList ( ) ) )
					.Batch ( V.PopulationSize )
					.Select ( x => x.ToList ( ) )
					.ToList ( );
				for ( var i = 0; i < generations.Count; i++ )
				{
					var generation = generations[i];
					Logger.Debug ( $"\n\nGen#{i + 1}\n" );
					foreach ( var (parent, offsprings) in generation )
					{
						Logger.Debug ( $"{parent} -> {offsprings.Count} Offsprings:" );
						foreach ( var offspring in offsprings )
							Logger.Debug ( $"\t{offspring} OffspringCount: {offspring.OffspringCount}" );
					}
				}

				Logger.Debug ( $"\n{Heritability}" );
			}
		}
	}
}