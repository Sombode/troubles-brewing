﻿' Trouble's Brewing (Final Project)
' 4-28-2023
' Period 1
' Levy Le

Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Drawing.Text
Imports System.IO 'TODO: Remove when no more debug is needed (haha...)

Public Class Form1

    Public gameObjects As LinkedList(Of gameObject) ' The main list of game objects (to loop through and tick/render them)
    Public newObjects, deadObjects As LinkedList(Of gameObject) ' Lists that will be used to modify gameObjects after iteration (as the direct list cannot be modified during iteration)
    Public mouseLock, grabLock As Boolean
    Public money As animatedValue = New animatedValue()
    ' TODO: Do something about right clicks?
    ' TODO: (Polish) rolling list of transactions (class); moves up and fades out

    Public pfc As PrivateFontCollection = New PrivateFontCollection() ' PFC and custom font code derived from: https://stackoverflow.com/questions/13573916/using-custom-fonts-in-my-winform-labels
    Public debugFont As Font

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        gameObjects = New LinkedList(Of gameObject)
        newObjects = New LinkedList(Of gameObject)
        deadObjects = New LinkedList(Of gameObject)
        mouseLock = False
        grabLock = False
        gameObjects.AddFirst(New order(800, 300))
        gameObjects.AddFirst(New order(900, 500))
        gameObjects.AddFirst(New order(1000, 700))
        gameObjects.AddFirst(New cauldron(500, 500))
        pfc.AddFontFile(Application.StartupPath.Replace("bin\Debug", "Resources\GermaniaOne-Regular.ttf")) ' An objectively bad way to import a custom font,
        ' but it works (as long as the project stays unpackaged) and I don't have to touch pointers (the alternative is AddMemoryFont, which is too complex).
        debugFont = New Font(pfc.Families(0), 14)
        money.setValue(1000000) ' obviously TEMP
    End Sub

    Private Sub tick()
        If mouseLock AndAlso (MouseButtons = MouseButtons.None) Then mouseLock = False
        For Each gameObj In gameObjects ' idea: combine tick and render into one loop? depends on interactions, would reduce iterations with more objects
            gameObj.tick()
        Next
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        End
    End Sub

    Private Sub Form1_Paint(sender As Object, e As PaintEventArgs) Handles Me.Paint

        e.Graphics.SmoothingMode = e.Graphics.SmoothingMode.AntiAlias
        'https://learn.microsoft.com/en-us/dotnet/api/system.drawing.graphics.smoothingmode?view=windowsdesktop-8.0
        ' Makes drawn graphics MUCH cleaner

        tick()
        ' According to https://stackoverflow.com/questions/57497422/how-to-stop-flickering-when-redrawing-ellipses-in-windows-forms,
        ' it is faster to use the Graphics from the PaintEventArgs of the Paint event rather than CreateGraphics(), which removes any flickering when redrawing

        ' Iterating through specific types (using OfType(Of ...)) so that types of objects are properly layered when drawn (cauldrons then orders then potions)

        For Each cauldron In gameObjects.OfType(Of cauldron).Reverse() ' Reversing these lists when rendering for 2 key reasons:
            cauldron.render(e.Graphics)
        Next

        For Each order In gameObjects.OfType(Of order).Reverse() ' 1 - So that draggable objects (which are grabbed with priority from the front of the list)
            ' are rendered in front of draggable objects with less priority (towards the back)
            order.render(e.Graphics)
        Next

        For Each potion In gameObjects.OfType(Of potion).Reverse() ' 2 - moveToFront() adds to the front (an easy change to fix but it works with reason 1 so I'm keeping it like this)
            potion.render(e.Graphics)
        Next

        ' Removing no longer needed objects post loop

        For Each deadObj In deadObjects
            gameObjects.Remove(deadObj)
        Next

        deadObjects.Clear()

        ' Adding in new objects post loop

        For Each newObj In newObjects
            gameObjects.AddFirst(newObj)
        Next

        newObjects.Clear()

        ' debug graphics
        e.Graphics.DrawString(Str(MousePosition.X) + ", " + Str(MousePosition.Y), debugFont, New SolidBrush(Color.Black), New Point(10, 10))
        e.Graphics.DrawString(Str(MouseButtons), debugFont, New SolidBrush(Color.Black), New Point(10, 50))
        e.Graphics.DrawString(Str(mouseLock), debugFont, New SolidBrush(Color.Black), New Point(10, 170))

        ' Money display

        Dim coin As Image = My.Resources.Coin
        Dim outline As Pen = New Pen(Color.Black, 10)

        outline.LineJoin = LineJoin.Round

        e.Graphics.DrawImage(coin, New Rectangle(5, Height - coin.Height - 5, coin.Width, coin.Height))
        outlineText(e.Graphics, FormatCurrency(money.getValue()), pfc.Families(0), 50, New SolidBrush(Color.White), outline, New Point(90, Height - 50), StringAlignment.Near)

        If (MouseButtons = MouseButtons.Left) Then mouseLock = True

        outline.Dispose()

        Invalidate() ' Marks the form's area as "invalid" so that it will be redrawn
    End Sub

    ' Below are useful functions used by most other classes (thus consolidated into Form1 for ease of access)

    Public Function concatRecipe(recipe As Integer(,)) As String ' A function to condense the recipe array into a workable object (string),
        ' which is easier to manipulate/compare; used throughout different classes
        Dim output As String = ""
        For stage = 0 To 2
            For ingredient = 0 To 3
                output += Str(recipe(stage, ingredient)) + ","
            Next
        Next
        output = output.Replace(" ", "") ' Remove whitespace (added by Str()?)
        Return output.Substring(0, output.Length - 1) ' Remove the last comma in the output
    End Function

    Public Sub outlineText(g As Graphics, text As String, font As FontFamily, fontSize As Single, fill As SolidBrush, outline As Pen, position As Point, alignment As Integer)
        ' Creates outlined text https://stackoverflow.com/questions/40310546/how-to-add-text-outline-to-button-text
        Dim textPath As GraphicsPath = New GraphicsPath()
        Dim strFormat As StringFormat = New StringFormat() ' https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/how-to-align-drawn-text?view=netframeworkdesktop-4.8
        strFormat.Alignment = alignment
        strFormat.LineAlignment = StringAlignment.Center
        textPath.AddString(text, font, 0, fontSize, position, strFormat)
        g.DrawPath(outline, textPath)
        g.FillPath(fill, textPath)
    End Sub

    Public Sub moveToFront(gameObj As gameObject) ' Moves an object to the front of its layer (based on type) by removing it from the list but readding it to the front
        newObjects.AddLast(gameObj) ' Adding to the back of the list so that all objects added are moved to the front in the right order (objects moved last are in front)
        deadObjects.AddLast(gameObj)
    End Sub

End Class

Public Class gameObject ' A basic class to be inherited by the main game objects, containing the fundimental tick() and render() subs

    Friend x, y As Integer
    Friend sprite As Image

    Public Sub New(x As Integer, y As Integer)
        Me.x = x
        Me.y = y
    End Sub

    Public Overridable Sub tick()

    End Sub

    Public Overridable Function getBounds() As Rectangle
        Return New Rectangle(x, y, sprite.Width, sprite.Height)
    End Function

    Public Overridable Sub render(g As Graphics)

        ' temp graphics
        Try
            g.DrawImage(sprite, getBounds()) ' Using getBounds() to create a rectangle with a defined width and height, as just using a point
            ' for position scaled the image
        Catch ex As Exception
            Console.WriteLine("No sprite!")
            Dim myPen As Pen = New Pen(Color.Red)
            g.DrawRectangle(myPen, x, y, 100, 100)
        End Try
    End Sub

End Class

Public Class draggable ' A class based off of gameObject with additional functionality for draggable objects
    Inherits gameObject

    Friend grabbed, grabPrimed As Boolean
    Friend grabOffset As Point

    Public Sub New(x As Integer, y As Integer)
        MyBase.New(x, y)
        grabbed = False
        grabPrimed = False
    End Sub

    Public Overrides Sub tick()
        MyBase.tick()
        If grabbed Then
            If Form1.MouseButtons = MouseButtons.Left Then
                x = Form1.MousePosition.X + grabOffset.X
                y = Form1.MousePosition.Y + grabOffset.Y
            Else
                grabbed = False
                Form1.grabLock = False
                onRelease()
            End If
        Else
            If Form1.grabLock Then Return ' Prevents multiple items from being grabbed at once
            If getBounds().Contains(Form1.MousePosition) Then
                If Form1.MouseButtons = MouseButtons.Left Then
                    If grabPrimed Then
                        grabbed = True
                        Form1.grabLock = True
                        grabOffset = Point.Subtract(New Point(x, y), Form1.MousePosition)
                        Form1.moveToFront(Me)
                        onGrab()
                    End If
                Else
                    grabPrimed = True
                End If
            Else
                grabPrimed = False
            End If

        End If
    End Sub

    Overridable Sub onGrab() ' Essentially event handlers for grabbing and releasing
    End Sub

    Overridable Sub onRelease()
    End Sub

End Class

Public Class animatedValue ' An object that can interpolate between two values for smooth transitions

    Public duration As Single
    Dim value, originalValue, difference, targetValue As Single
    Dim frame As Integer

    Public Sub New()
        value = 0
        duration = 10
        frame = 0
    End Sub

    Public Sub New(startValue As Single)
        Me.New()
        value = startValue
    End Sub

    Public Function getValue() As Single
        If frame >= duration Then Return value
        frame += 1
        value = originalValue + (difference * (frame / duration)) ' (frame / duration) provides a t from 0 to 1 (the whole progression of the animation),
        ' which is then scaled by the difference needed to cover
        Return value
    End Function

    Public Function rawValue() As Single
        Return value
    End Function

    Public Sub setValue(target As Single)
        If targetValue = target Then Return ' Do nothing if the value is already moving to the correct value
        originalValue = value
        difference = target - value
        targetValue = target
        frame = 0
    End Sub

    Public Sub snapValue(target As Single)
        frame = duration
        value = target
        originalValue = target
        targetValue = target
    End Sub

    Public Sub addValue(number As Single)
        originalValue = value
        difference = number
        targetValue = value + difference
        frame = 0
    End Sub

End Class

Public Class potion
    Inherits draggable

    Dim potionColor() As Single
    Dim recipe(0 To 2, 0 To 3) As Integer
    Dim selectedOrder As order

    Public Sub New(x As Integer, y As Integer)
        MyBase.New(x, y)
        sprite = My.Resources.PotionBase
        potionColor = {Rnd(), Rnd(), Rnd(), 0, 1}
        selectedOrder = Nothing
    End Sub

    Public Sub New(x As Integer, y As Integer, offset As Point)
        MyBase.New(x, y)
        grabbed = True
        Form1.grabLock = True
        grabOffset = offset
        sprite = My.Resources.PotionBase
        potionColor = {Rnd(), Rnd(), Rnd(), 0, 1}
        selectedOrder = Nothing
        ' TODO: Remove unecessary constructors
    End Sub

    Public Sub New(x As Integer, y As Integer, offset As Point, recipe As Integer(,))
        MyBase.New(x, y)
        grabbed = True
        Form1.grabLock = True
        grabOffset = offset
        sprite = My.Resources.PotionBase
        Me.recipe = recipe
        selectedOrder = Nothing

        ' "Hashes" the potion color based on its ingredients

        Dim condensedRecipe() As Single = { ' Sums the total amounts of each ingredient (regardless of the stage which they were added)
            recipe(0, 0) + recipe(1, 0) + recipe(2, 0),
            recipe(0, 1) + recipe(1, 1) + recipe(2, 1),
            recipe(0, 2) + recipe(1, 2) + recipe(2, 2),
            recipe(0, 3) + recipe(1, 3) + recipe(2, 3)
        }

        Dim normRecipe() As Single = normalize(condensedRecipe)

        ' Potion color palette derived (with modification) from: https://colorhunt.co/palette/c6d57ed57e7ea2cdcdffe1af
        ' Base colors: #E67E7E (Red: Shroom), #C6E67E (Green: Herb), #E6E67E (Yellow: Eye), #9AB8E6 (Blue: Crystal)

        Dim baseColors()() As Single = { ' Jagged array for the four main colors: https://learn.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/arrays/#jagged-arrays
            (New Single() {230, 126, 126}), ' Red #E67E7E
            (New Single() {198, 230, 126}), ' Green #C6E67E
            (New Single() {230, 230, 126}), ' Yellow #E6E67E
            (New Single() {154, 184, 230})  ' Blue #9AB8E6
        }

        Dim finalColor As Single() = {0, 0, 0}

        For i = 0 To 3
            Dim scaledColor() As Single = colorScale(baseColors(i), normRecipe(i))
            For h = 0 To 2
                finalColor(h) += scaledColor(h)
            Next
        Next

        finalColor = colorScale(normalize(finalColor), 230 / 255)

        potionColor = {finalColor(0), finalColor(1), finalColor(2), 0, 1}

    End Sub

    Public Overrides Sub tick()
        MyBase.tick()
        If grabbed Then
            selectedOrder = Nothing ' https://learn.microsoft.com/en-us/dotnet/visual-basic/language-reference/nothing
            For Each order In Form1.gameObjects.OfType(Of order)
                order.potionHover = False
                If order.getPaperRect().Contains(Form1.MousePosition) Then
                    selectedOrder = order
                    order.potionHover = True
                    Exit For
                End If
            Next
        End If
    End Sub

    Public Overrides Sub onRelease()
        MyBase.onRelease()
        If Not IsNothing(selectedOrder) Then
            ' Selling a potion
            If Form1.concatRecipe(recipe) = Form1.concatRecipe(selectedOrder.recipe) Then
                Form1.money.addValue(1000)
            Else
                Form1.money.addValue(-1000)
            End If
            Form1.deadObjects.AddLast(selectedOrder)
            Form1.deadObjects.AddLast(Me)
        End If
    End Sub

    Private Function colorScale(color() As Single, scalar As Single) As Single()
        ' A function to iterate through an array and multiply all values by a scalar (used for color blending)
        Dim newColor() As Single = color
        For i = 0 To color.Count - 1
            newColor(i) = color(i) * scalar
        Next
        Return newColor
    End Function

    Private Function normalize(base() As Single) As Single()
        ' "Normalizes" an array by dividing all components by its maximum value, allowing for the ratio between
        ' values to remain constant while the actual values are altered
        Dim maxVal As Single = base.Max
        Dim normVals(base.Count - 1) As Single
        For i = 0 To base.Count - 1
            normVals(i) = base(i) / maxVal
        Next
        Return normVals
    End Function

    Public Overrides Sub render(g As Graphics)
        MyBase.render(g)
        ' Color transform code derived from: https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/how-to-translate-image-colors?view=netframeworkdesktop-4.8
        Dim imgAttr As ImageAttributes = New ImageAttributes ' The ImageAttributes class applies a "filter" to images when drawn
        imgAttr.SetColorMatrix(New ColorMatrix({
                                                New Single() {1, 0, 0, 0, 0}, ' All red values are multiplied by 1 (kept same)  
                                                New Single() {0, 1, 0, 0, 0}, ' All green values are multiplied by 1 (kept same)  
                                                New Single() {0, 0, 1, 0, 0}, ' All blue values are multiplied by 1 (kept same)  
                                                New Single() {0, 0, 0, 1, 0}, ' All alpha values are multiplied by 1 (kept same)  
                                                potionColor}), ColorMatrixFlag.Default, ColorAdjustType.Bitmap) ' Values will be "translated" (added with) this array (color)
        ' The potion liquid sprite is black as adding color via transforms will add to the base, allowing for any color to be made by adding the right values
        g.DrawImage(My.Resources.PotionLiquid, getBounds(), 0, 0, sprite.Width, sprite.Height, GraphicsUnit.Point, imgAttr)
        ' Drawing an image with ImageAttributes also needs a "source rectangle" (part of the image that will be displayed) and a GraphicsUnit

        ' TODO: fix sprite to remove gaps (enlarge liquid)
        If grabbed Then
            g.TranslateTransform(x + sprite.Width / 2, y + sprite.Height / 2)
            g.RotateTransform(45)
            g.DrawImage(My.Resources.PotionStopper, New Rectangle(-sprite.Width / 2, -sprite.Height / 2, sprite.Width, sprite.Height))
            ' Potion "rotates" on pickup (liquid and gleam are independent of rotation, so they don't move)
        Else
            g.DrawImage(My.Resources.PotionStopper, getBounds())
        End If
        g.ResetTransform()
    End Sub

End Class

Public Class cauldron
    Inherits gameObject

    Dim brewStart As DateTime
    Dim potionMenuActive As Boolean
    Dim totalTime As Integer = 30000
    Dim brewStage As Integer ' -1 for no brew, 0 for green, 1 for yellow, 2 for red
    Dim ingredientHistory As LinkedList(Of Integer())
    Dim recipe(0 To 2, 0 To 3) As Integer
    ' Ingredients put into the cauldron will be stored in three separate "arrays" (based on the first index of the recipe array;
    ' one for each "stage" of brewing). The amount of the four hardcoded ingredients will be stored in the second index in the following order (CW):
    ' Shroom, Herb, Eye, Crystal

    Public Sub New(x As Integer, y As Integer)
        MyBase.New(x, y)
        ingredientHistory = New LinkedList(Of Integer())
        recipe = {{0, 0, 0, 0}, {0, 0, 0, 0}, {0, 0, 0, 0}} ' Here it may be easier to see how the ingredient amounts are stored,
        ' split into three separate "subarrays" for each of the three brewing stages.
        sprite = My.Resources.Cauldron
        potionMenuActive = False
        brewStage = -1
    End Sub

    Private Sub checkStartBrew() ' Small sub to check if a cauldron is empty to start brewing once an ingredient is added
        If brewStage = -1 AndAlso recipe(0, 0) + recipe(0, 1) + recipe(0, 2) + recipe(0, 3) = 0 Then ' Adding up the values of the first four items
            brewStart = DateTime.Now    ' to effectively check if they all equal 0 (otherwise the sum is not 0). Only checking the first 4 as brewing
            brewStage = 0               ' does not start at any other stage (slightly more efficient than just calling and using concatRecipe())
        End If
    End Sub

    Private Function getBrewTime() As Integer
        If brewStage = -1 Then Return -1
        Dim timeSpan As TimeSpan = DateTime.Now.TimeOfDay - brewStart.TimeOfDay
        Return timeSpan.TotalMilliseconds
    End Function

    Private Sub cleanArc(g As Graphics, brush As Brush, x As Integer, y As Integer, outer As Integer, inner As Integer, startAngle As Integer, sweepAngle As Integer)
        ' Alternative to g.drawArc, but without misplaced endcaps (drawing an arc from 0 to 180 would not result in a flat edges on the semicircle)
        ' https://stackoverflow.com/questions/36096759/how-to-draw-a-circular-progressbar-pie-using-graphicspath-in-winform
        Dim arcPath As GraphicsPath = New GraphicsPath()
        With arcPath
            .AddArc(New Rectangle(CInt(x - outer / 2), CInt(y - outer / 2), outer, outer), startAngle, sweepAngle) ' Outer edge of the arc
            .AddArc(New Rectangle(CInt(x - inner / 2), CInt(y - inner / 2), inner, inner), startAngle + sweepAngle, -sweepAngle) ' Inner edge of the arc
            .CloseFigure()
        End With
        g.FillPath(brush, arcPath)
    End Sub

    Private Sub outlineText(g As Graphics, text As String, font As FontFamily, fontSize As Single, fill As SolidBrush, outline As Pen, position As Point)
        ' https://stackoverflow.com/questions/40310546/how-to-add-text-outline-to-button-text
        Dim textPath As GraphicsPath = New GraphicsPath()
        outline.LineJoin = LineJoin.Bevel
        ' Fixes a glitch where the 1 and 4 glyphs are shaped in such a way that the corners create sharp spikes ("thorns") by rounding corners
        ' https://stackoverflow.com/questions/44683841/infinity-angles-on-sharp-corners-in-graphics
        textPath.AddString(text, font, 0, fontSize, position, StringFormat.GenericDefault)
        g.DrawPath(outline, textPath)
        g.FillPath(fill, textPath)
    End Sub

    Public Overrides Sub tick()
        MyBase.tick()
        If Not brewStage = -1 Then
            If getBrewTime() > Math.Round(2 * totalTime / 3) Then ' Updating the brew stage
                brewStage = 2
            ElseIf getBrewTime() > Math.Round(totalTime / 3) Then
                brewStage = 1
            Else
                brewStage = 0
            End If
        End If
    End Sub

    Public Overrides Sub render(g As Graphics)
        MyBase.render(g)

        ' Constants consistently used for rendering
        Dim darkBrush As SolidBrush = New SolidBrush(Color.FromArgb(200, 10, 10, 10))
        Dim lightBrush As SolidBrush = New SolidBrush(Color.FromArgb(200, 200, 200, 200))
        Dim centerX As Integer = x + CInt(getBounds().Width / 2)
        Dim centerY As Integer = y + CInt(getBounds().Height / 2)

        ' Rendering the brew stage dial
        If Not brewStage = -1 Then
            Dim dial = My.Resources.BrewDial
            Dim arrow = My.Resources.BrewDialArrow
            g.DrawImage(dial, x + 82, y - 55, 86, 40)
            g.TranslateTransform(centerX, y - 25)
            g.RotateTransform(-90) ' Rotate starts from left
            g.RotateTransform(Math.Min(getBrewTime() * 180 / totalTime, 190)) ' Uses Math.Min to stop the arrow from going past the dial's end
            g.DrawImage(arrow, -CInt(arrow.Width / 2) + 1, -22, arrow.Width, arrow.Height)
            g.ResetTransform()
        End If

        ' Rendering debug stats
        'g.DrawString(concatRecipe(), Form1.debugFont, New SolidBrush(Color.Black), New Point(x + 260, y))
        outlineText(g, Form1.concatRecipe(recipe), Form1.pfc.Families(0), 24, New SolidBrush(Color.White), New Pen(Color.Black, 6), New Point(x + 260, y))
        g.DrawString(Str(getBrewTime()), Form1.debugFont, New SolidBrush(Color.Black), New Point(x + 260, y + 40))
        g.DrawString(Str(brewStage), Form1.debugFont, New SolidBrush(Color.Black), New Point(x + 260, y + 80))

        If Form1.grabLock Then Return ' Don't render/handle cauldron interactions if something is grabbed/dragged

        If getBounds().Contains(Form1.MousePosition) Then
            Dim mouseDist = Math.Sqrt((centerX - Form1.MousePosition.X) ^ 2 + (centerY - Form1.MousePosition.Y) ^ 2)
            g.DrawString(Str(mouseDist), Form1.debugFont, New SolidBrush(Color.Black), New Point(10, 90))
            If Not brewStage = -1 Then g.FillEllipse(darkBrush, centerX - 30, centerY - 30, 60, 60)
            If potionMenuActive Then
                ' Rendering potion bottling event (a arrow bounces back and forth, timing the bottling correctly rewards extra money)
                ' this event will be known as a QTE in the code (quicktime event, probably a misnomer)

                ' The bottom half of the circle (actual button where the player clicks to bottle a potion)
                cleanArc(g, darkBrush, centerX, centerY + 5, 225, 75, 0, 180)

                ' The top half of the circle (the actual QTE with the arrow) (halves are offset in height to avoid overlap)
                cleanArc(g, darkBrush, centerX, centerY - 5, 225, 75, -180, 180)
                cleanArc(g, New SolidBrush(Color.FromArgb(100, 30, 30, 30)), centerX, centerY - 5, 225, 75, -180, 180)
                cleanArc(g, New SolidBrush(Color.FromArgb(100, 70, 70, 70)), centerX, centerY - 5, 225, 75, -135, 90)
                cleanArc(g, New SolidBrush(Color.FromArgb(100, 110, 110, 110)), centerX, centerY - 5, 225, 75, -105, 30)
                ' The different arcs represent the different "regions" of the circle, increasingly narrow to the center

                If mouseDist > 40 And mouseDist < 110 Then
                    Dim mouseAngle = Math.Atan2(y + 100 - Form1.MousePosition.Y, x + 125 - Form1.MousePosition.X)
                    g.DrawString(Str(mouseAngle), Form1.debugFont, New SolidBrush(Color.Black), New Point(10, 130))
                    If mouseAngle < 0 And mouseAngle > -Math.PI Then
                        cleanArc(g, lightBrush, centerX, centerY + 5, 225, 75, 0, 180)
                        If Not Form1.mouseLock AndAlso (Form1.MouseButtons = MouseButtons.Left) Then
                            ' TODO: Add code to actually make the QTE matter to potion quality
                            Form1.newObjects.AddFirst(New potion(Form1.MousePosition.X - 50, Form1.MousePosition.Y - 50, New Point(-50, -50), recipe))
                            ' Resetting the cauldron's state
                            recipe = {{0, 0, 0, 0}, {0, 0, 0, 0}, {0, 0, 0, 0}}
                            potionMenuActive = False
                            brewStage = -1
                        End If
                    End If
                End If

                cleanArc(g, New SolidBrush(Color.FromArgb(255, 200, 200, 200)), centerX, centerY - 5, 225, 75, -CDec(Math.Abs(180 - ((getBrewTime() / 5) Mod 360))) - 3, 6)
                ' The QTE arrow is rendered as a small arc with a start angle that oscillates from -180 to 0 (by getting the absolute difference between 180
                ' time mod 360 so that it smoothly goes back and forth (0 and 360 will produce the same value, allowing for the transition to be seamless)

            Else
                ' Rendering the ingredient wheel
                cleanArc(g, darkBrush, centerX, centerY, 225, 75, 0, 360)
                If mouseDist > 40 And mouseDist < 110 Then
                    Dim mouseAngle = Math.Atan2(y + 100 - Form1.MousePosition.Y, x + 125 - Form1.MousePosition.X)
                    g.DrawString(Str(mouseAngle), Form1.debugFont, New SolidBrush(Color.Black), New Point(10, 130))
                    If mouseAngle <= (Math.PI / 4) And mouseAngle >= (-Math.PI / 4) Then ' Left Section (Crystal)
                        cleanArc(g, lightBrush, centerX, centerY, 225, 75, 135, 90)
                        If Not Form1.mouseLock AndAlso (Form1.MouseButtons = MouseButtons.Left) Then
                            checkStartBrew()
                            recipe(brewStage, 3) += 1
                            ingredientHistory.AddLast({3, CInt(Math.Min(getBrewTime() * 180 / totalTime, 190))})
                        End If
                    ElseIf mouseAngle > (Math.PI / 4) And mouseAngle < (3 * Math.PI / 4) Then ' Top Section (Shroom)
                        cleanArc(g, lightBrush, centerX, centerY, 225, 75, 225, 90)
                        If Not Form1.mouseLock AndAlso (Form1.MouseButtons = MouseButtons.Left) Then
                            checkStartBrew()
                            recipe(brewStage, 0) += 1
                            ingredientHistory.AddLast({0, CInt(Math.Min(getBrewTime() * 180 / totalTime, 190))})
                        End If
                    ElseIf mouseAngle >= (3 * Math.PI / 4) Or mouseAngle <= (-3 * Math.PI / 4) Then ' Right Section (Herb)
                        cleanArc(g, lightBrush, centerX, centerY, 225, 75, -45, 90)
                        If Not Form1.mouseLock AndAlso (Form1.MouseButtons = MouseButtons.Left) Then
                            checkStartBrew()
                            recipe(brewStage, 1) += 1
                            ingredientHistory.AddLast({1, CInt(Math.Min(getBrewTime() * 180 / totalTime, 190))})
                        End If
                    ElseIf mouseAngle < (-Math.PI / 4) And mouseAngle > (-3 * Math.PI / 4) Then ' Bottom Section (Eye)
                        cleanArc(g, lightBrush, centerX, centerY, 225, 75, 45, 90)
                        If Not Form1.mouseLock AndAlso (Form1.MouseButtons = MouseButtons.Left) Then
                            checkStartBrew()
                            recipe(brewStage, 2) += 1
                            ingredientHistory.AddLast({2, CInt(Math.Min(getBrewTime() * 180 / totalTime, 190))})
                        End If
                    End If
                End If
                g.DrawImage(My.Resources.Shroom, New Rectangle(centerX - 30, centerY - 105, 60, 60))
                g.DrawImage(My.Resources.Herb, New Rectangle(centerX + 45, centerY - 30, 60, 60))
                g.DrawImage(My.Resources.Eye, New Rectangle(centerX - 30, centerY + 45, 60, 60))
                g.DrawImage(My.Resources.Crystal, New Rectangle(centerX - 105, centerY - 30, 60, 60))

                ' Ingredient history

                cleanArc(g, New SolidBrush(Color.Green), centerX, centerY, 325, 300, 120, 60)
                cleanArc(g, New SolidBrush(Color.Yellow), centerX, centerY, 325, 300, 60, 60)
                cleanArc(g, New SolidBrush(Color.Red), centerX, centerY, 325, 300, 0, 60)

                ' PROGRESS MARKER: Ingredients and angles are stored in ingredient history, render them here

            End If
            If Not brewStage = -1 Then
                If potionMenuActive Then
                    g.DrawImage(My.Resources.CancelIcon, New Rectangle(centerX - 29, centerY - 29, 60, 60))
                Else
                    g.DrawImage(My.Resources.PotionIcon, New Rectangle(centerX - 29, centerY - 29, 60, 60))
                End If
                If mouseDist < 30 Then
                    g.FillEllipse(lightBrush, centerX - 30, centerY - 30, 60, 60)
                    If Not Form1.mouseLock AndAlso (Form1.MouseButtons = MouseButtons.Left) Then potionMenuActive = Not potionMenuActive
                End If
            End If
        End If

        darkBrush.Dispose()
        lightBrush.Dispose()
    End Sub

End Class

Public Class order
    Inherits draggable

    Public potionHover As Boolean
    Dim orderOpen As Boolean ' Kept independently of potion-hovering effects (which can temporarily open an order)
    Dim maxHeight As Integer
    Dim orderHeight As animatedValue = New animatedValue()
    Public recipe(0 To 2, 0 To 3) As Integer

    Public Sub New(x As Integer, y As Integer)
        MyBase.New(x, y)
        sprite = My.Resources.PaperRoll
        orderHeight.snapValue(40)
        orderOpen = False
        potionHover = False
        Randomize()
        recipe = {{Int(Rnd() * 3), Int(Rnd() * 3), Int(Rnd() * 3), Int(Rnd() * 3)}, {Int(Rnd() * 3), Int(Rnd() * 3), Int(Rnd() * 3), Int(Rnd() * 3)}, {Int(Rnd() * 3), Int(Rnd() * 3), Int(Rnd() * 3), Int(Rnd() * 3)}}
    End Sub

    Public Function getPaperRect() As Rectangle
        Return New Rectangle(x, y + 19, My.Resources.OrderBackground.Width, orderHeight.getValue())
    End Function

    Public Overrides Sub tick()
        MyBase.tick()
        If Not grabbed AndAlso Not Form1.mouseLock AndAlso
            New Rectangle(x, y + orderHeight.getValue(), sprite.Width, sprite.Height).Contains(Form1.MousePosition) And
            Form1.MouseButtons = MouseButtons.Left Then

            orderOpen = Not orderOpen
        End If
        If potionHover Then
            orderHeight.duration = 3
            orderHeight.setValue(275)
        Else
            orderHeight.duration = 10
            If orderOpen Then
                orderHeight.setValue(275)
            Else
                orderHeight.setValue(40)
            End If
        End If
    End Sub

    Public Overrides Sub render(g As Graphics)
        'If Form1.MouseButtons = MouseButtons.Right Then orderHeight += 5
        Dim bg As Image = My.Resources.OrderBackground
        Dim ingredients() As Image = {My.Resources.Shroom, My.Resources.Herb, My.Resources.Eye, My.Resources.Crystal}
        Dim textBrush As SolidBrush = New SolidBrush(Color.White)
        Dim textPen As Pen = New Pen(Color.Black, 5)
        textPen.LineJoin = LineJoin.Bevel ' Fixes a glitch where the 1 and 4 glyphs are shaped in such a way that the corners create sharp spikes ("thorns") by rounding corners
        ' https://stackoverflow.com/questions/44683841/infinity-angles-on-sharp-corners-in-graphics

        g.SetClip(getPaperRect())

        g.FillRectangle(New SolidBrush(Color.FromArgb(201, 162, 103)), New Rectangle(x, y + 19, My.Resources.OrderBackground.Width, 300)) ' Renders a blank background if the orderBg 
        ' sprite does not render in time with opening a recipe

        If orderHeight.getValue() > 50 Then
            g.DrawImage(bg, New Rectangle(x, y + 19, bg.Width, bg.Height))
            For h = 0 To 2 ' Iterating through brew stages (columns)
                Dim emptyOffset As Integer = 0
                For k = 0 To 3 ' Iterating (rendering) through all four ingredients (in a column)
                    If recipe(h, k) = 0 Then
                        emptyOffset += 1 ' If an ingredient shouldn't be added, shift back the y offset of all subsequent ingredients in the column by 1 (45)
                    Else
                        g.DrawImage(ingredients(k), New Rectangle(x + 20 + 64 * h, y + 80 + 45 * (k - emptyOffset), 40, 40))
                        Form1.outlineText(g, recipe(h, k), Form1.pfc.Families(0), 20, textBrush, textPen, New Point(x + 62.5 + 64 * h, y + 110 + 45 * (k - emptyOffset)), StringAlignment.Far)
                    End If
                Next
            Next
        Else
        End If

        g.ResetClip()

        Dim bottomRoll As Image = My.Resources.RollUp
        If Not (orderOpen Or potionHover) Then bottomRoll = My.Resources.RollDown
        g.DrawImage(bottomRoll, New Rectangle(x, y + orderHeight.getValue(), sprite.Width, sprite.Height))

        MyBase.render(g)

        If potionHover And orderHeight.getValue > 274 Then g.DrawImage(My.Resources.SellOverlay, New Rectangle(x, y, sprite.Width, sprite.Height + 275))

        textBrush.Dispose()
        textPen.Dispose()
    End Sub

End Class