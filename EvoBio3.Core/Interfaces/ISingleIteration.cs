﻿using System.Collections.Generic;
using EvoBio3.Core.Enums;

namespace EvoBio3.Core.Interfaces
{
	public interface ISingleIteration<TIndividual, TGroup, TVariables>
		where TIndividual : class, IIndividual, new ( )
		where TGroup : IIndividualGroup<TIndividual>, new ( )
		where TVariables : IVariables
	{
		bool IsLoggingEnabled { get; set; }
		TVariables V { get; }
		TGroup[] AllGroups { get; }
		IList<TIndividual> AllIndividuals { get; }
		TGroup Cooperator1Group { get; }
		TGroup Cooperator2Group { get; }
		TGroup ResonationGroup { get; }
		TGroup DefectorGroup { get; }
		int Step1PerishCount { get; }
		int Step2PerishCount { get; }
		IList<TIndividual> Step1Rejects { get; }
		IList<TIndividual> Step2Rejects { get; }
		IList<TIndividual> Step1Survivors { get; }
		IList<TIndividual> Step2Survivors { get; }
		int TotalPerished { get; }
		IHeritabilitySummary Heritability { get; }
		Winner Winner { get; }
		int GenerationsPassed { get; }
		double ReservationQualityCutoffForCooperator1Version1 { get; }
		double ReservationQualityCutoffForCooperator2Version1 { get; }
		double ResonationQualityCutoffForCooperator1WithNoReservationVersion1 { get; }
		double ResonationQualityCutoffForCooperator2WithNoReservationVersion1 { get; }
		double ResonationQualityCutoffForResonationTypeVersion1 { get; }
		double ResonationQualityCutoffForCooperator1WithReservationVersion1 { get; }
		double ResonationQualityCutoffForCooperator2WithReservationVersion1 { get; }

		IAdjustmentRules<TIndividual, TGroup, TVariables,
			ISingleIteration<TIndividual, TGroup, TVariables>> AdjustmentRules { get; }

		IDictionary<IndividualType, List<int>> GenerationHistory { get; }

		void Init (
			TVariables v,
			IAdjustmentRules<TIndividual, TGroup, TVariables,
				ISingleIteration<TIndividual, TGroup, TVariables>> adjustmentRules = null,
			bool isLoggingEnabled = false );

		void ResetLists ( );
		void CalculateThresholds ( );
		void CreateInitialPopulation ( );
		void Perish1 ( );
		void Perish2 ( );
		void CalculateFecundity ( );
		void CalculateAdjustedFecundity ( );
		List<TIndividual> GetParents ( );
		void ChooseParentsAndReproduce ( );
		void CalculateHeritability ( );

		bool SimulateGeneration ( );
		void Run ( );
	}
}