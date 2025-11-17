import { CKEditor } from '@ckeditor/ckeditor5-react'
import '@ckeditor5-static/ckeditor5.css'
import '@ckeditor5-static/ckeditor5-editor.css'
import '@ckeditor5-static/ckeditor5-content.css'
import '@ckeditor5-static/translations/fa.js'
import './ckeditor.css'
import {
  ClassicEditor,
  AccessibilityHelp,
  Alignment,
  AlignmentEditing,
  Autoformat,
  Autosave,
  BlockQuote,
  Bold,
  Essentials,
  FontBackgroundColor,
  FontColor,
  FontFamily,
  FontSize,
  Heading,
  Indent,
  IndentBlock,
  Italic,
  Link,
  List,
  ListProperties,
  Mention,
  Paragraph,
  PasteFromMarkdownExperimental,
  PasteFromOffice,
  RemoveFormat,
  SelectAll,
  SpecialCharacters,
  SpecialCharactersArrows,
  SpecialCharactersCurrency,
  SpecialCharactersEssentials,
  SpecialCharactersLatin,
  SpecialCharactersMathematical,
  SpecialCharactersText,
  Table,
  TableCaption,
  TableCellProperties,
  TableColumnResize,
  TableProperties,
  TableToolbar,
  TextTransformation,
  TodoList,
  Underline,
  Undo,
  WordCount,
} from '@ckeditor5-local'

type Props = { value: string; onChange: (html: string) => void }

const editorConfig = {
  language: 'fa',
  placeholder: 'توضیحات محصول را اینجا بنویسید...',
  plugins: [
    AccessibilityHelp,
    Autoformat,
    Autosave,
    Alignment,
    AlignmentEditing,
    BlockQuote,
    Bold,
    Essentials,
    FontBackgroundColor,
    FontColor,
    FontFamily,
    FontSize,
    Heading,
    Indent,
    IndentBlock,
    Italic,
    Link,
    List,
    ListProperties,
    Mention,
    Paragraph,
    PasteFromMarkdownExperimental,
    PasteFromOffice,
    RemoveFormat,
    SelectAll,
    SpecialCharacters,
    SpecialCharactersArrows,
    SpecialCharactersCurrency,
    SpecialCharactersEssentials,
    SpecialCharactersLatin,
    SpecialCharactersMathematical,
    SpecialCharactersText,
    Table,
    TableCaption,
    TableCellProperties,
    TableColumnResize,
    TableProperties,
    TableToolbar,
    TextTransformation,
    TodoList,
    Underline,
    Undo,
    WordCount,
  ],
  toolbar: {
    items: [
      'undo',
      'redo',
      '|',
      'heading',
      '|',
      'fontSize',
      'fontFamily',
      'fontColor',
      'fontBackgroundColor',
      '|',
      'bold',
      'italic',
      'underline',
      'removeFormat',
      '|',
      'specialCharacters',
      'link',
      'insertTable',
      'blockQuote',
      '|',
      'bulletedList',
      'alignment',
      'numberedList',
      'todoList',
      'outdent',
      'indent',
    ],
    shouldNotGroupWhenFull: true,
  },
  fontFamily: {
    supportAllValues: true,
  },
  fontSize: {
    options: [8, 10, 12, 14, 'default', 18, 20, 22],
    supportAllValues: true,
  },
  heading: {
    options: [
      { model: 'paragraph', title: 'Paragraph', class: 'ck-heading_paragraph' },
      { model: 'heading1', view: 'h1', title: 'Heading 1', class: 'ck-heading_heading1' },
      { model: 'heading2', view: 'h2', title: 'Heading 2', class: 'ck-heading_heading2' },
      { model: 'heading3', view: 'h3', title: 'Heading 3', class: 'ck-heading_heading3' },
      { model: 'heading4', view: 'h4', title: 'Heading 4', class: 'ck-heading_heading4' },
      { model: 'heading5', view: 'h5', title: 'Heading 5', class: 'ck-heading_heading5' },
      { model: 'heading6', view: 'h6', title: 'Heading 6', class: 'ck-heading_heading6' },
    ],
  },
  list: {
    properties: {
      styles: true,
      startIndex: true,
      reversed: true,
    },
  },
  link: {
    addTargetToExternalLinks: true,
    defaultProtocol: 'https://',
  },
  mention: {
    feeds: [
      {
        marker: '@',
        feed: [],
      },
    ],
  },
  menuBar: {
    isVisible: true,
  },
  table: {
    contentToolbar: ['tableColumn', 'tableRow', 'mergeTableCells', 'tableProperties', 'tableCellProperties'],
  },
}

export function DescriptionEditor({ value, onChange }: Props) {
  return (
    <div className="space-y-2 ck-editor-wrapper">
      <div className="label mb-1">توضیحات محصول</div>
      <div className="border rounded bg-white ck-editor-container">
        <CKEditor
          editor={ClassicEditor as any}
          data={value || ''}
          config={editorConfig}
          onReady={(editor) => {
            const editable = editor.ui.view.editable.element
            if (editable) {
              editable.style.minHeight = '520px'
              editable.style.padding = '20px'
              editable.style.fontSize = '16px'
              editable.style.lineHeight = '1.8'
            }
          }}
          onChange={(_, editor) => {
            onChange(editor.getData())
          }}
        />
      </div>
    </div>
  )
}
