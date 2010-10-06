using System;
using System.Collections.Generic;
using NGit.Diff;
using NGit.Merge;
using Sharpen;

namespace NGit.Merge
{
	/// <summary>
	/// Provides the merge algorithm which does a three-way merge on content provided
	/// as RawText.
	/// </summary>
	/// <remarks>
	/// Provides the merge algorithm which does a three-way merge on content provided
	/// as RawText. Makes use of
	/// <see cref="NGit.Diff.MyersDiff{S}">NGit.Diff.MyersDiff&lt;S&gt;</see>
	/// to compute the diffs.
	/// </remarks>
	public sealed class MergeAlgorithm
	{
		/// <summary>
		/// Since this class provides only static methods I add a private default
		/// constructor to prevent instantiation.
		/// </summary>
		/// <remarks>
		/// Since this class provides only static methods I add a private default
		/// constructor to prevent instantiation.
		/// </remarks>
		public MergeAlgorithm()
		{
		}

		private static readonly Edit END_EDIT = new Edit(int.MaxValue, int.MaxValue);

		// An special edit which acts as a sentinel value by marking the end the
		// list of edits
		/// <summary>Does the three way merge between a common base and two sequences.</summary>
		/// <remarks>Does the three way merge between a common base and two sequences.</remarks>
		/// <?></?>
		/// <param name="cmp">comparison method for this execution.</param>
		/// <param name="base">the common base sequence</param>
		/// <param name="ours">the first sequence to be merged</param>
		/// <param name="theirs">the second sequence to be merged</param>
		/// <returns>the resulting content</returns>
		public static MergeResult<S> Merge<S>(SequenceComparator<S> cmp, S @base, S ours, 
			S theirs) where S:Sequence
		{
			IList<S> sequences = new AList<S>(3);
			sequences.AddItem(@base);
			sequences.AddItem(ours);
			sequences.AddItem(theirs);
			MergeResult<S> result = new MergeResult<S>(sequences);
			EditList oursEdits = MyersDiff<S>.INSTANCE.Diff(cmp, @base, ours);
			Iterator<Edit> baseToOurs = oursEdits.Iterator();
			EditList theirsEdits = MyersDiff<S>.INSTANCE.Diff(cmp, @base, theirs);
			Iterator<Edit> baseToTheirs = theirsEdits.Iterator();
			int current = 0;
			// points to the next line (first line is 0) of base
			// which was not handled yet
			Edit oursEdit = NextEdit(baseToOurs);
			Edit theirsEdit = NextEdit(baseToTheirs);
			// iterate over all edits from base to ours and from base to theirs
			// leave the loop when there are no edits more for ours or for theirs
			// (or both)
			while (theirsEdit != END_EDIT || oursEdit != END_EDIT)
			{
				if (oursEdit.GetEndA() <= theirsEdit.GetBeginA())
				{
					// something was changed in ours not overlapping with any change
					// from theirs. First add the common part in front of the edit
					// then the edit.
					if (current != oursEdit.GetBeginA())
					{
						result.Add(0, current, oursEdit.GetBeginA(), MergeChunk.ConflictState.NO_CONFLICT
							);
					}
					result.Add(1, oursEdit.GetBeginB(), oursEdit.GetEndB(), MergeChunk.ConflictState.
						NO_CONFLICT);
					current = oursEdit.GetEndA();
					oursEdit = NextEdit(baseToOurs);
				}
				else
				{
					if (theirsEdit.GetEndA() <= oursEdit.GetBeginA())
					{
						// something was changed in theirs not overlapping with any
						// from ours. First add the common part in front of the edit
						// then the edit.
						if (current != theirsEdit.GetBeginA())
						{
							result.Add(0, current, theirsEdit.GetBeginA(), MergeChunk.ConflictState.NO_CONFLICT
								);
						}
						result.Add(2, theirsEdit.GetBeginB(), theirsEdit.GetEndB(), MergeChunk.ConflictState
							.NO_CONFLICT);
						current = theirsEdit.GetEndA();
						theirsEdit = NextEdit(baseToTheirs);
					}
					else
					{
						// here we found a real overlapping modification
						// if there is a common part in front of the conflict add it
						if (oursEdit.GetBeginA() != current && theirsEdit.GetBeginA() != current)
						{
							result.Add(0, current, Math.Min(oursEdit.GetBeginA(), theirsEdit.GetBeginA()), MergeChunk.ConflictState
								.NO_CONFLICT);
						}
						// set some initial values for the ranges in A and B which we
						// want to handle
						int oursBeginB = oursEdit.GetBeginB();
						int theirsBeginB = theirsEdit.GetBeginB();
						// harmonize the start of the ranges in A and B
						if (oursEdit.GetBeginA() < theirsEdit.GetBeginA())
						{
							theirsBeginB -= theirsEdit.GetBeginA() - oursEdit.GetBeginA();
						}
						else
						{
							oursBeginB -= oursEdit.GetBeginA() - theirsEdit.GetBeginA();
						}
						// combine edits:
						// Maybe an Edit on one side corresponds to multiple Edits on
						// the other side. Then we have to combine the Edits of the
						// other side - so in the end we can merge together two single
						// edits.
						//
						// It is important to notice that this combining will extend the
						// ranges of our conflict always downwards (towards the end of
						// the content). The starts of the conflicting ranges in ours
						// and theirs are not touched here.
						//
						// This combining is an iterative process: after we have
						// combined some edits we have to do the check again. The
						// combined edits could now correspond to multiple edits on the
						// other side.
						//
						// Example: when this combining algorithm works on the following
						// edits
						// oursEdits=((0-5,0-5),(6-8,6-8),(10-11,10-11)) and
						// theirsEdits=((0-1,0-1),(2-3,2-3),(5-7,5-7))
						// it will merge them into
						// oursEdits=((0-8,0-8),(10-11,10-11)) and
						// theirsEdits=((0-7,0-7))
						//
						// Since the only interesting thing to us is how in ours and
						// theirs the end of the conflicting range is changing we let
						// oursEdit and theirsEdit point to the last conflicting edit
						Edit nextOursEdit = NextEdit(baseToOurs);
						Edit nextTheirsEdit = NextEdit(baseToTheirs);
						for (; ; )
						{
							if (oursEdit.GetEndA() > nextTheirsEdit.GetBeginA())
							{
								theirsEdit = nextTheirsEdit;
								nextTheirsEdit = NextEdit(baseToTheirs);
							}
							else
							{
								if (theirsEdit.GetEndA() > nextOursEdit.GetBeginA())
								{
									oursEdit = nextOursEdit;
									nextOursEdit = NextEdit(baseToOurs);
								}
								else
								{
									break;
								}
							}
						}
						// harmonize the end of the ranges in A and B
						int oursEndB = oursEdit.GetEndB();
						int theirsEndB = theirsEdit.GetEndB();
						if (oursEdit.GetEndA() < theirsEdit.GetEndA())
						{
							oursEndB += theirsEdit.GetEndA() - oursEdit.GetEndA();
						}
						else
						{
							theirsEndB += oursEdit.GetEndA() - theirsEdit.GetEndA();
						}
						// A conflicting region is found. Strip off common lines in
						// in the beginning and the end of the conflicting region
						int conflictLen = Math.Min(oursEndB - oursBeginB, theirsEndB - theirsBeginB);
						int commonPrefix = 0;
						while (commonPrefix < conflictLen && cmp.Equals(ours, oursBeginB + commonPrefix, 
							theirs, theirsBeginB + commonPrefix))
						{
							commonPrefix++;
						}
						conflictLen -= commonPrefix;
						int commonSuffix = 0;
						while (commonSuffix < conflictLen && cmp.Equals(ours, oursEndB - commonSuffix - 1
							, theirs, theirsEndB - commonSuffix - 1))
						{
							commonSuffix++;
						}
						conflictLen -= commonSuffix;
						// Add the common lines at start of conflict
						if (commonPrefix > 0)
						{
							result.Add(1, oursBeginB, oursBeginB + commonPrefix, MergeChunk.ConflictState.NO_CONFLICT
								);
						}
						// Add the conflict
						if (conflictLen > 0)
						{
							result.Add(1, oursBeginB + commonPrefix, oursEndB - commonSuffix, MergeChunk.ConflictState
								.FIRST_CONFLICTING_RANGE);
							result.Add(2, theirsBeginB + commonPrefix, theirsEndB - commonSuffix, MergeChunk.ConflictState
								.NEXT_CONFLICTING_RANGE);
						}
						// Add the common lines at end of conflict
						if (commonSuffix > 0)
						{
							result.Add(1, oursEndB - commonSuffix, oursEndB, MergeChunk.ConflictState.NO_CONFLICT
								);
						}
						current = Math.Max(oursEdit.GetEndA(), theirsEdit.GetEndA());
						oursEdit = nextOursEdit;
						theirsEdit = nextTheirsEdit;
					}
				}
			}
			// maybe we have a common part behind the last edit: copy it to the
			// result
			if (current < @base.Size())
			{
				result.Add(0, current, @base.Size(), MergeChunk.ConflictState.NO_CONFLICT);
			}
			return result;
		}

		/// <summary>Helper method which returns the next Edit for an Iterator over Edits.</summary>
		/// <remarks>
		/// Helper method which returns the next Edit for an Iterator over Edits.
		/// When there are no more edits left this method will return the constant
		/// END_EDIT.
		/// </remarks>
		/// <param name="it">the iterator for which the next edit should be returned</param>
		/// <returns>
		/// the next edit from the iterator or END_EDIT if there no more
		/// edits
		/// </returns>
		private static Edit NextEdit(Iterator<Edit> it)
		{
			return (it.HasNext() ? it.Next() : END_EDIT);
		}
	}
}