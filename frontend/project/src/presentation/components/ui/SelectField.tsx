import { forwardRef, useId, type CSSProperties, type SelectHTMLAttributes } from 'react';
import { ChevronDown } from 'lucide-react';
import clsx from 'clsx';
import styles from './SelectField.module.css';

interface SelectFieldProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label: string;
  error?: string;
  hint?: string;
  /** Applied to the field's wrapper <div>, not the raw <select>, so margin/layout props work as expected. */
  style?: CSSProperties;
}

export const SelectField = forwardRef<HTMLSelectElement, SelectFieldProps>(
  ({ label, error, hint, id, className, style, children, ...rest }, ref) => {
    const autoId = useId();
    const fieldId = id ?? autoId;
    const hintId = `${fieldId}-hint`;
    const errorId = `${fieldId}-error`;

    return (
      <div className={clsx(styles.field, className)} style={style}>
        <label htmlFor={fieldId} className={styles.label}>
          {label}
        </label>
        <div className={styles.selectWrapper}>
          <select
            ref={ref}
            id={fieldId}
            className={clsx(styles.select, error && styles.selectError)}
            aria-invalid={Boolean(error)}
            aria-describedby={error ? errorId : hint ? hintId : undefined}
            {...rest}
          >
            {children}
          </select>
          <ChevronDown size={16} className={styles.chevron} aria-hidden />
        </div>
        {hint && !error && (
          <span id={hintId} className={styles.hint}>
            {hint}
          </span>
        )}
        {error && (
          <span id={errorId} className={styles.error} role="alert">
            {error}
          </span>
        )}
      </div>
    );
  },
);

SelectField.displayName = 'SelectField';
