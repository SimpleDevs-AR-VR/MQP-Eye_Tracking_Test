import os
import numpy as np
import pandas as pd
from scipy import stats

def import_trials(pdir:str):
    df = pd.read_csv(os.path.join(pdir, 'trials.csv'))
    trials_df = df[df['trial_index']>=0]
    trials = {t: pd.read_csv(os.path.join(pdir, f'{t}.csv')) for t in trials_df['trial_name'].tolist()}
    return trials_df, trials

def load_steps_from_trial(df):
    # load steps within the trial
    start_indices = df.index[df['event']=='Target Loaded'].tolist()
    end_indices = start_indices[1:] + [len(df)]
    steps = [df.iloc[start:end] for start,end in zip(start_indices,end_indices)]
    return steps

def load_halves_from_steps(steps):
    # Pre-define output array + columns we are interested in
    halves = []
    filter_cols = ['unix_ms', 'event', 'avg_target_diff', 'head_target_diff', 'avg_head_diff']
    
    # Iterate through each step, which should amount to 9 for a trial
    for step_df in steps:
        # Get the row indices for the loaded, transitioning, and set events
        phase0_idx = step_df.index[step_df['event'] == 'Target Loaded'].tolist()
        phase1_idx = step_df.index[step_df['event'] == 'Targit Transitioning'].tolist()
        phase2_idx = step_df.index[step_df['event'] == 'Target Set'].tolist()
        
        # If the indices are not null, store what the indices are
        phase0_start = phase0_idx[0] if phase0_idx else None
        phase1_start = phase1_idx[0] if phase1_idx else None
        phase2_start = phase2_idx[0] if phase2_idx else None

        # Define the output dfs we'll be storing in `halves`
        phase0_df = pd.DataFrame(columns=filter_cols)
        phase1_df = pd.DataFrame(columns=filter_cols)
        phase2_df = pd.DataFrame(columns=filter_cols)

        if (phase1_start is not None and phase2_start is not None):
            # Both halves are present
            phase0_df = step_df.loc[[phase0_start,phase1_start,phase2_start]][filter_cols]
            phase0_df['step_ms'] = phase0_df['unix_ms'] - phase0_df.loc[phase0_start]['unix_ms']
            phase1_df = step_df.loc[phase1_start:phase2_start-1][filter_cols]
            phase1_df['step_ms'] = phase1_df['unix_ms'] - phase0_df.loc[phase0_start]['unix_ms']
            phase2_df = step_df.loc[phase2_start:][filter_cols]
            phase2_df['step_ms'] = phase2_df['unix_ms'] - phase0_df.loc[phase0_start]['unix_ms']
        elif phase2_start is not None:
            # Only second half present
            phase0_df = step_df.loc[[phase0_start,phase2_start]][filter_cols]
            phase0_df['step_ms'] = phase0_df['unix_ms'] - phase0_df.loc[phase0_start]['unix_ms']
            phase2_df = step_df.loc[phase2_start:][filter_cols]
            phase2_df['step_ms'] = phase2_df['unix_ms'] - phase0_df.loc[phase0_start]['unix_ms']
        elif phase1_start is not None:
            # Only first half is present
            phase0_df = step_df.loc[[phase0_start, phase1_start]][filter_cols]
            phase0_df['step_ms'] = phase0_df['unix_ms'] - phase0_df.loc[phase0_start]['unix_ms']
            phase1_df = step_df.loc[phase1_start:][filter_cols]
            phase1_df['step_ms'] = phase1_df['unix_ms'] - phase0_df.loc[phase0_start]['unix_ms']
        
        # store the halves + timestamps for events
        halves.append((phase1_df, phase2_df, phase0_df))
    
    # return halves
    return halves

def calculate_target_diffs(steps):
    # Initialize
    angular_diffs = []
    prev_vector = None

    # Iterate through each step_df
    for step_df in steps:
        # Find the "Target Loaded" row
        start_row = step_df[step_df['event'] == 'Target Loaded']
        # Edge case: if no start row, just enter NaN
        if start_row.empty:
            angular_diffs.append(np.nan)
            continue
        # Extract x,y,z from this row
        vector = start_row[['calib_dir_x','calib_dir_y','calib_dir_z']].values[0]
        if prev_vector is None:
            angular_diffs.append(0.0)
        else:
            # Compute angle in radians
            dp = np.dot(prev_vector, vector)
            norm_product = np.linalg.norm(prev_vector) * np.linalg.norm(vector)
            # prevent numerical issues
            cos_theta = np.clip(dp / norm_product, -1.0, 1.0)
            angle_rad = np.arccos(cos_theta)
            # Convert into degrees
            angle_deg = np.degrees(angle_rad)
            # Add to angular_diffs
            angular_diffs.append(angle_deg)
        # Update previous vector
        prev_vector = vector
    return angular_diffs

def calculate_performance(halves, angular_diffs):
    # halves[step_index][0] = target transitioning
    # halves[step_index][1] = target set
    # We ignore the first point
    means = []
    sds = []
    rmses = []
    for step_index in range(1,len(halves)):
        step_df = halves[step_index][1]
        # To remove saccades and general latency, filter first second and only look at the next either 0.5 or 1 second, depending
        step_df = step_df[step_df['step_ms'].between(1000,2000)]
        # only include relevant columns
        diff_cols = ['avg_target_diff','head_target_diff','avg_head_diff']
        step_df = step_df[diff_cols]
        # Filter to only within 3 SDs to remove outliers
        step_df = step_df[(np.abs(stats.zscore(step_df)) < 3).all(axis=1)]
        # Calculate the average of each reamining column
        _means = step_df.mean()
        _means['target_diff'] = angular_diffs[step_index]
        _means.name = step_index
        means.append(_means)
        # Calculate the SD of each remaining column
        _sd = step_df.std()
        _sd['target_diff'] = angular_diffs[step_index]
        _sd.neam = step_index
        sds.append(_sd)
        # Calculate the RMS of each remaining column
        rmse_per_feature = np.sqrt((step_df ** 2).mean())
        rmse_per_feature['target_diff'] = angular_diffs[step_index]
        rmse_per_feature.name = step_index
        rmses.append(rmse_per_feature)
    mean_df = pd.DataFrame(means)
    sd_df = pd.DataFrame(sds)
    rmse_df = pd.DataFrame(rmses)
    return mean_df, sd_df, rmse_df